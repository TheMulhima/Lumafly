using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using JetBrains.Annotations;
using MessageBox.Avalonia.BaseWindows.Base;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.Models;
using Microsoft.Extensions.DependencyInjection;
using Scarab.Interfaces;
using Scarab.Models;
using Scarab.Services;
using Scarab.Util;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using PropertyChanged.SourceGenerator;

namespace Scarab.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
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

        public static MainWindowViewModel? Instance;

        [UsedImplicitly]
        private ViewModelBase Content => SelectedTabIndex < 0 ? new LoadingViewModel() : Tabs[SelectedTabIndex].ViewModel;
        public IBrush BorderBrush => new SolidColorBrush(Color.FromRgb(0x28, 0x28, 0x28));
        public Thickness BorderThickness => new(1);
        public CornerRadius CornerRadius => new(3);
        public string AppName => "Scarab+";

        [Notify]
        private ObservableCollection<TabItemModel> _tabs = new ObservableCollection<TabItemModel>();

        [Notify]
        private int _selectedTabIndex = -1;

        private async Task Impl()
        {
            SelectedTabIndex = -1;
            
            Trace.WriteLine($"Opening Scarab Version: {Assembly.GetExecutingAssembly().GetName().Version}");
            Trace.WriteLine("Checking if up to date...");
            
            await CheckUpToDate();
            
            var sc = new ServiceCollection();
            var fs = new FileSystem();

            Trace.WriteLine("Loading settings.");
            Settings settings = Settings.Load() ?? Settings.Create(await GetSettingsPath());

            if (!PathUtil.ValidateExisting(settings.ManagedFolder))
                settings = await ResetSettings();

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

            HttpClient hc = new HttpClient();
            ScarabMode scarabMode;
            
            var modLinksCache = Path.Combine(Settings.ConfigFolderPath, "Modlinks.xml");
            var apiLinksCache = Path.Combine(Settings.ConfigFolderPath, "ApiLinks.xml");
            
            try
            {
                var res = await WorkaroundHttpClient.TryWithWorkaroundAsync(
                    settings.RequiresWorkaroundClient 
                        ? HttpSetting.OnlyWorkaround
                        : HttpSetting.TryBoth,
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
                scarabMode = ScarabMode.Online;
                await CacheModAndApiLinksForOffline(modLinksCache, apiLinksCache, content);
            }
            catch (Exception e) when (e is TaskCanceledException { CancellationToken.IsCancellationRequested: true } or HttpRequestException)
            {
                string failedOp = e switch
                {
                    TaskCanceledException => Resources.MWVM_Impl_Error_Fetch_ModLinks_Timeout,
                    HttpRequestException when e.ToString().Contains("No such host is known.")
                        => string.Format(Resources.MWVM_Impl_Error_Fetch_ModLinks_Error, Resources.MVVM_DNSError),
                    HttpRequestException http => string.Format(Resources.MWVM_Impl_Error_Fetch_ModLinks_Error, http.StatusCode),
                    _ => throw new ArgumentOutOfRangeException()
                };
                
                var linksCacheExists = File.Exists(modLinksCache) && File.Exists(apiLinksCache);

                if (!linksCacheExists)
                {
                    await MessageBoxUtil.GetMessageBoxStandardWindow
                    (
                        title: Resources.MWVM_Impl_Error_Fetch_ModLinks_Msgbox_Title,
                        text: string.Format(Resources.MWVM_Impl_Error_Fetch_ModLinks_Msgbox_Text, failedOp),
                        icon: Icon.Error
                    ).Show();
                    throw;
                }

                var offlineMode = await MessageBoxUtil.GetMessageBoxStandardWindow
                (
                    title: Resources.MVVM_UnableToGetModlinks,
                    text: string.Format(Resources.MWVM_Impl_Error_Fetch_ModLinks_Msgbox_Text, failedOp) + "\n\n" +
                          $"{Resources.MVVM_LaunchOfflineMode}",
                    icon: Icon.Warning,
                    @enum: ButtonEnum.YesNo
                ).Show() == ButtonResult.Yes;

                if (!offlineMode) throw;

                content = (ModDatabase.FromString<ModLinks>(await File.ReadAllTextAsync(modLinksCache)), 
                    ModDatabase.FromString<ApiLinks>(await File.ReadAllTextAsync(apiLinksCache)));
                
                scarabMode = ScarabMode.Offline;
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
              .AddSingleton<IGlobalSettingsFinder, GlobalSettingsFinder>()
              .AddSingleton<IFileSystem>(_ => fs)
              .AddSingleton<IModSource>(_ => installedMods)
              .AddSingleton<IModDatabase, ModDatabase>(sp 
                  => new ModDatabase(sp.GetRequiredService<IModSource>(),sp.GetRequiredService<IGlobalSettingsFinder>(), content, settings))
              .AddSingleton<IInstaller, Installer>()
              .AddSingleton<ModListViewModel>(sp =>
                  new ModListViewModel(sp.GetRequiredService<ISettings>(),
                      sp.GetRequiredService<IModDatabase>(),
                      sp.GetRequiredService<IInstaller>(),
                      sp.GetRequiredService<IModSource>(),
                      sp.GetRequiredService<IGlobalSettingsFinder>(),
                      scarabMode))
              .AddSingleton<SettingsViewModel>();
            
            Trace.WriteLine("Building service provider");
            ServiceProvider sp = sc.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true
            });
            Trace.WriteLine("Built service provider");

            Trace.WriteLine("Displaying model");
            Tabs = new ObservableCollection<TabItemModel>
            {
                new(Resources.XAML_Mods, sp.GetRequiredService<ModListViewModel>()),
                new(Resources.XAML_Settings, sp.GetRequiredService<SettingsViewModel>()),
            };
            SelectedTabIndex = 0;
        }

        private async Task CacheModAndApiLinksForOffline(string modLinksCache, string apiLinksCache, (ModLinks ml, ApiLinks al) content)
        {
            try
            {
                await File.WriteAllTextAsync(modLinksCache, content.ml.Raw);
                await File.WriteAllTextAsync(apiLinksCache, content.al.Raw);
            }
            catch (Exception e)
            {
                Trace.TraceError($"Unable to cache modlinks or apilinks {e}");
            }
        }

        private static async Task EnsureAccessToConfigFile()
        {
            if (!File.Exists(InstalledMods.ConfigPath)) return;
            try
            {
                bool configAccessSuccess = false;
                while (!configAccessSuccess)
                {
                    try
                    {
                        var configFile = File.Open(InstalledMods.ConfigPath, FileMode.Open, FileAccess.ReadWrite,
                            FileShare.None);
                        configFile.Close();
                        configAccessSuccess = true;
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

                        await MessageBoxUtil.GetMessageBoxStandardWindow
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
            
            string? res = await MessageBoxUtil.GetMessageBoxCustomWindow
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

            await MessageBoxUtil.GetMessageBoxStandardWindow
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
                IMsBoxWindow<ButtonResult> info = MessageBoxUtil.GetMessageBoxStandardWindow
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

            IMsBoxWindow<ButtonResult> window = MessageBoxUtil.GetMessageBoxStandardWindow
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
        
        public MainWindowViewModel()
        {
            Instance = this;
            LoadApp();
        }

        public void LoadApp() => Dispatcher.UIThread.InvokeAsync(async () =>
        {
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
