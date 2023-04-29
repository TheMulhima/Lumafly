using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using JetBrains.Annotations;
using MessageBox.Avalonia;
using MessageBox.Avalonia.BaseWindows.Base;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.Models;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Scarab.Interfaces;
using Scarab.Models;
using Scarab.Services;
using Scarab.Util;
using Scarab.Views;
using Avalonia.Media;

namespace Scarab.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private static bool _Debug
        {
            get {
                #if DEBUG
                return true;
                #else
                return false;
                #endif
            }
        }

        private ViewModelBase _content = null!;

        [UsedImplicitly]
        private ViewModelBase Content
        {
            get => _content;
            set => this.RaiseAndSetIfChanged(ref _content, value);
        }
        public IBrush BorderBrush => new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20));
        public Thickness BorderThickness => new(1);
        public CornerRadius CornerRadius => new(3);
        public string AppName => "Scarab";

        private async Task Impl()
        {
            Trace.WriteLine($"Opening Scarab Version: {Assembly.GetExecutingAssembly().GetName().Version}");
            Trace.WriteLine("Checking if up to date...");
            
            await CheckUpToDate();
            
            var sc = new ServiceCollection();
            var fs = new FileSystem();

            Trace.WriteLine("Loading settings.");
            Settings settings = Settings.Load() ?? Settings.Create(await GetSettingsPath());

            if (!PathUtil.ValidateExisting(settings.ManagedFolder))
                settings = await ResetSettings();

            // instantiate the singleton GlobalSettingsFinder 
            var _ = new GlobalSettingsFinder(settings);

            await EnsureAccessToConfigFile();

            Trace.WriteLine("Fetching links");
            
            (ModLinks ml, ApiLinks al) content;

            void AddSettings(HttpClient hc)
            {
                hc.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true,
                    MustRevalidate = true
                };
                
                hc.DefaultRequestHeaders.Add("User-Agent", "Scarab");
            }

            HttpClient hc;
            
            try
            {
                var res = await WorkaroundHttpClient.TryWithWorkaroundAsync(
                    settings.RequiresWorkaroundClient 
                        ? WorkaroundHttpClient.Settings.OnlyWorkaround
                        : WorkaroundHttpClient.Settings.TryBoth,
                    ModDatabase.FetchContent,
                    AddSettings
                );

                content = res.Result;

                if (res.NeededWorkaround && !settings.RequiresWorkaroundClient)
                {
                    settings.RequiresWorkaroundClient = true;
                    settings.Save();
                }

                hc = res.Client;
            }
            catch (Exception e) when (e is TaskCanceledException { CancellationToken.IsCancellationRequested: true } or HttpRequestException)
            {
                string failedOp = e switch
                {
                    TaskCanceledException => Resources.MWVM_Impl_Error_Fetch_ModLinks_Timeout,
                    HttpRequestException when e.ToString().Contains("No such host is known.")
                        => string.Format(Resources.MWVM_Impl_Error_Fetch_ModLinks_Error, "Possibly caused by poor or no internet connection. Please check that and try again"),
                    HttpRequestException http => string.Format(Resources.MWVM_Impl_Error_Fetch_ModLinks_Error, http.StatusCode),
                    _ => throw new ArgumentOutOfRangeException()
                };
                
                await MessageBoxManager.GetMessageBoxStandardWindow
                (
                    title: Resources.MWVM_Impl_Error_Fetch_ModLinks_Msgbox_Title,
                    text: string.Format(Resources.MWVM_Impl_Error_Fetch_ModLinks_Msgbox_Text, failedOp),
                    icon: Icon.Error
                ).Show();

                throw;
            }

            Trace.WriteLine("Fetched links successfully");

            var installedMods = await InstalledMods.Load(
                fs,
                settings,
                content.ml
            );
            
            sc
              .AddSingleton(hc)
              .AddSingleton<ISettings>(_ => settings)
              .AddSingleton<IFileSystem>(_ => fs)
              .AddSingleton<IModSource>(_ => installedMods)
              .AddSingleton<IModDatabase, ModDatabase>(sp => new ModDatabase(sp.GetRequiredService<IModSource>(), content, settings))
              .AddSingleton<IInstaller, Installer>()
              .AddSingleton<ModListViewModel>();
            
            Trace.WriteLine("Building service provider");
            ServiceProvider sp = sc.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true
            });
            Trace.WriteLine("Built service provider");

            Trace.WriteLine("Displaying model");
            Content = sp.GetRequiredService<ModListViewModel>();
        }

        private static async Task EnsureAccessToConfigFile()
        {
            try
            {
                bool configAccessSucess = false;
                while (!configAccessSucess)
                {
                    try
                    {
                        var configFile = File.Open(InstalledMods.ConfigPath, FileMode.Open, FileAccess.ReadWrite,
                            FileShare.None);
                        configFile.Close();
                        configAccessSucess = true;
                    }
                    catch (IOException)
                    {
                        string additionalInfo = "";
                        try
                        {
                            if (OperatingSystem.IsWindows())
                            {
                                var processes = FileAccessLookup.WhoIsLocking(InstalledMods.ConfigPath);
                                additionalInfo =
                                    $"\n\nProcesses that are locking the file:\n{string.Join("\n", processes.Select(x => x.ProcessName))}";
                            }
                        }
                        catch (Exception)
                        {
                            //ignored as its not a requirement
                        }

                        await MessageBoxManager.GetMessageBoxStandardWindow
                        (
                            title: "File access error!!",
                            text: $"Scarab cannot run without being able to access {InstalledMods.ConfigPath}.\n" +
                                  $"Please close any other apps that could be using that" + additionalInfo,
                            icon: Icon.Error
                        ).Show();
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError($"Error happened when trying to find out if config files are locked. {e}");
            }
        }

        private static async Task CheckUpToDate()
        {
            Version? current_version = Assembly.GetExecutingAssembly().GetName().Version;
            
            Debug.WriteLine($"Current version of installer is {current_version}");

            if (_Debug)
                return; 

            const string gh_releases = "https://api.github.com/repos/TheMulhima/Scarab/releases/latest";

            string json;
            
            try
            {
                var hc = new HttpClient();
                
                hc.DefaultRequestHeaders.Add("User-Agent", "Scarab");

                json = await hc.GetStringAsync(new Uri(gh_releases));
            }
            catch (Exception e) when (e is HttpRequestException or TimeoutException) {
                return;
            }

            JsonDocument doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("tag_name", out JsonElement tag_elem))
                return;

            string? tag = tag_elem.GetString();

            if (tag is null)
                return;

            if (tag.StartsWith("v"))
                tag = tag[1..];

            if (!Version.TryParse(tag.Length == 1 ? tag + ".0.0.0" : tag, out Version? version))
                return;

            if (version <= current_version)
                return;
            
            string? res = await MessageBoxManager.GetMessageBoxCustomWindow
            (
                new MessageBoxCustomParams {
                    ButtonDefinitions = new[] {
                        new ButtonDefinition {
                            IsDefault = true,
                            IsCancel = true,
                            Name = Resources.MWVM_OutOfDate_GetLatest
                        },
                        new ButtonDefinition {
                            Name = Resources.MWVM_OutOfDate_ContinueAnyways
                        }
                    },
                    ContentTitle = Resources.MWVM_OutOfDate_Title,
                    ContentMessage = Resources.MWVM_OutOfDate_Message,
                    SizeToContent = SizeToContent.WidthAndHeight
                }
            ).Show();

            if (res == Resources.MWVM_OutOfDate_GetLatest)
            {
                Process.Start(new ProcessStartInfo("https://github.com/TheMulhima/Scarab/releases/latest") { UseShellExecute = true });
                
                ((IClassicDesktopStyleApplicationLifetime?) Application.Current?.ApplicationLifetime)?.Shutdown();
            }
            else
            {
                Trace.WriteLine($"Installer out of date! Version {current_version} with latest {version}!");
            }
        }
        
        private static async Task<Settings> ResetSettings()
        {
            Trace.WriteLine("Settings path is invalid, forcing re-selection.");

            await MessageBoxManager.GetMessageBoxStandardWindow
            (
                new MessageBoxStandardParams {
                    ContentHeader = Resources.MWVM_Warning,
                    ContentMessage = Resources.MWVM_InvalidSavedPath_Message,
                    // The auto-resize for this lib is buggy, so 
                    // ensure that the message doesn't get cut off 
                    MinWidth = 550
                }
            ).Show();

            return Settings.Create(await GetSettingsPath());
        }

        private static async Task<string> GetSettingsPath()
        {
            if (!Settings.TryAutoDetect(out ValidPath? path))
            {
                IMsBoxWindow<ButtonResult> info = MessageBoxManager.GetMessageBoxStandardWindow
                (
                    new MessageBoxStandardParams
                    {
                        ContentHeader = Resources.MWVM_Info,
                        ContentMessage = Resources.MWVM_UnableToDetect_Message,
                        MinWidth = 550
                    }
                );

                await info.Show();
                
                return await PathUtil.SelectPath();
            }

            Trace.WriteLine($"Settings doesn't exist. Creating it at detected path {path}.");

            IMsBoxWindow<ButtonResult> window = MessageBoxManager.GetMessageBoxStandardWindow
            (
                new MessageBoxStandardParams
                {
                    ContentHeader = Resources.MWVM_DetectedPath_Title,
                    ContentMessage = string.Format(Resources.MWVM_DetectedPath_Message, path.Root),
                    ButtonDefinitions = ButtonEnum.YesNo
                }
            );

            ButtonResult res = await window.Show();

            return res == ButtonResult.Yes
                ? Path.Combine(path.Root, path.Suffix)
                : await PathUtil.SelectPath();
        }

        public MainWindowViewModel() => Dispatcher.UIThread.InvokeAsync(async () =>
        {
            Content = new LoadingViewModel();
            try
            {
                await Impl();
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Trace.Flush();

                if (Debugger.IsAttached)
                    Debugger.Break();
                
                Environment.Exit(-1);
                
                throw;
            }
        });
    }
}
