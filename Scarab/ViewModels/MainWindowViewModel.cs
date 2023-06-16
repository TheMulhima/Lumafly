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
using System.Threading;
using System.Threading.Tasks;
using PropertyChanged.SourceGenerator;
using Scarab.Enums;

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

        private static bool isFirstLoad { get; set; } = true;
        public static MainWindowViewModel? Instance { get; private set; }

        [UsedImplicitly]
        private ViewModelBase Content => Loading || SelectedTabIndex < 0 ? new LoadingViewModel() : Tabs[SelectedTabIndex].Item;
        public IBrush BorderBrush => new SolidColorBrush(Color.FromRgb(0x28, 0x28, 0x28));
        public Thickness BorderThickness => new(1);
        public CornerRadius CornerRadius => new(3);
        public string AppName => "Scarab+";

        [Notify]
        private ObservableCollection<SelectableItem<ViewModelBase>> _tabs = new ObservableCollection<SelectableItem<ViewModelBase>>();

        [Notify]
        private int _selectedTabIndex = -1;

        [Notify]
        private bool _loading = true;

        private async Task Impl()
        {
            Trace.WriteLine($"Opening Scarab Version: {Assembly.GetExecutingAssembly().GetName().Version}");
            
            var urlSchemeHandler = new UrlSchemeHandler(handled: !isFirstLoad);

            HandleURLSchemeCommand(urlSchemeHandler);

            Trace.WriteLine("Checking if up to date...");
            
            await CheckUpToDate();
            
            var sc = new ServiceCollection();
            var fs = new FileSystem();

            Trace.WriteLine("Loading settings.");

            HandleResetUrlScheme(urlSchemeHandler);
            HandleResetAllGlobalSettingsUrlScheme(urlSchemeHandler);

            Settings settings = Settings.Load() ?? Settings.Create(await GetSettingsPath());

            if (!PathUtil.ValidateExisting(settings.ManagedFolder))
                settings = await ResetSettings();

            await EnsureAccessToConfigFile();

            HandleLinkUrlScheme(settings, urlSchemeHandler);

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
                ResultInfo<(ModLinks, ApiLinks)>? res = null;

                if (settings.UseCustomModlinks)
                {
                    try
                    {
                        res = await WorkaroundHttpClient.TryWithWorkaroundAsync(
                            settings.RequiresWorkaroundClient
                                ? HttpSetting.OnlyWorkaround
                                : HttpSetting.TryBoth,
                            f => ModDatabase.FetchContent(f, settings, fetchOfficial: false),
                            AddSettings);
                    }
                    catch (InvalidModlinksException)
                    {
                        await MessageBoxUtil.GetMessageBoxStandardWindow(
                            Resources.MVVM_InvalidCustomModlinks_Header,
                            string.Format(Resources.MVVM_InvalidCustomModlinks_Body, settings.CustomModlinksUri),
                            icon: Icon.Error
                        ).Show();
                    }
                }

                // if above failed or skipped, res will be null
                
                res ??= await WorkaroundHttpClient.TryWithWorkaroundAsync(
                    settings.RequiresWorkaroundClient
                        ? HttpSetting.OnlyWorkaround
                        : HttpSetting.TryBoth,
                    f => ModDatabase.FetchContent(f, settings, fetchOfficial: true),
                    AddSettings);


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

            Trace.WriteLine("Creating service collection");
            sc
              .AddSingleton<IUrlSchemeHandler>(_ => urlSchemeHandler)
              .AddSingleton(hc)
              .AddSingleton<ISettings>(_ => settings)
              .AddSingleton<IGlobalSettingsFinder, GlobalSettingsFinder>()
              .AddSingleton<ICheckValidityOfAssembly, CheckValidityOfAssembly>()
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
                      sp.GetRequiredService<IUrlSchemeHandler>(),
                      scarabMode))
              .AddSingleton<SettingsViewModel>()
              .AddSingleton<InfoViewModel>();
            
            Trace.WriteLine("Building service provider");
            ServiceProvider sp = sc.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true
            });
            Trace.WriteLine("Built service provider");

            Trace.WriteLine("Displaying model");
            Tabs = new ObservableCollection<SelectableItem<ViewModelBase>>
            {
                new(sp.GetRequiredService<InfoViewModel>(), Resources.XAML_Info, false),
                new(sp.GetRequiredService<ModListViewModel>(), Resources.XAML_Mods, false),
                new(sp.GetRequiredService<SettingsViewModel>(), Resources.XAML_Settings, false),
            };
            SelectedTabIndex = 0;
            Trace.WriteLine("Selected Tab 0");
        }

        private void HandleURLSchemeCommand(IUrlSchemeHandler urlSchemeHandler)
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length == 2) // only accept 2 args, the exe location and the uri 
            {
                urlSchemeHandler.SetCommand(args[1]);
            }
        }

        private void HandleResetUrlScheme(IUrlSchemeHandler urlSchemeHandler)
        {
            if (urlSchemeHandler is { Handled: false, UrlSchemeCommand: UrlSchemeCommands.reset })
            {
                bool success = false;
                Exception? exception = null; 
                try
                {
                    DirectoryInfo di = new DirectoryInfo(Settings.GetOrCreateDirPath());

                    foreach (FileInfo file in di.GetFiles())
                    {
                        // we can't delete the currently being used logging file
                        if (file.Name == Program.LoggingFileName) continue;
                        file.Delete(); 
                    }
                    
                    success = true;
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                    success = false;
                    exception = e;
                }

                Dispatcher.UIThread.InvokeAsync(async () => await urlSchemeHandler.ShowConfirmation(
                    title: "Reset installer from command",
                    message: success ? "The installer has been reset." : $"The installer could not be reset. Please try again.\n{exception}",
                    success ? Icon.Success : Icon.Warning
                ));
            }
        }
        
        private void HandleResetAllGlobalSettingsUrlScheme(IUrlSchemeHandler urlSchemeHandler)
        {
            if (urlSchemeHandler is { Handled: false, UrlSchemeCommand: UrlSchemeCommands.removeAllModsGlobalSettings })
            {
                bool success = false;
                Exception? exception = null; 
                try
                {
                    var di = new DirectoryInfo(GlobalSettingsFinder.GetSavesFolder());

                    foreach (var file in di.GetFiles())
                    {
                        if (file.FullName.EndsWith(".GlobalSettings.json") ||
                            file.FullName.EndsWith(".GlobalSettings.json.bak"))
                        {
                            file.Delete();
                        } 
                    }
                    
                    success = true;
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                    success = false;
                    exception = e;
                }

                Dispatcher.UIThread.InvokeAsync(async () => await urlSchemeHandler.ShowConfirmation(
                    title: "Reset all mod global settings installer from command",
                    message: success ? "All mods global settings have been reset." : $"All mods global settings could not be reset. Please try again.\n{exception}",
                    success ? Icon.Success : Icon.Warning));
            }
        }

        private void HandleLinkUrlScheme(ISettings settings, IUrlSchemeHandler urlSchemeHandler)
        {
            if (!urlSchemeHandler.Handled)
            {
                if (urlSchemeHandler.UrlSchemeCommand == UrlSchemeCommands.customModLinks)
                {
                    bool success = false;
                    if (string.IsNullOrEmpty(urlSchemeHandler.Data))
                    {
                        Trace.TraceError($"{UrlSchemeCommands.customModLinks}:{urlSchemeHandler.Data} not found");
                        success = false;
                    }
                    else
                    {
                        settings.UseCustomModlinks = true;
                        settings.CustomModlinksUri = urlSchemeHandler.Data;
                        success = true;
                    }

                    Dispatcher.UIThread.InvokeAsync(async () => await urlSchemeHandler.ShowConfirmation(
                        title:  "Load custom modlinks from command", 
                        message: success ? $"Got the custom modlinks \"{settings.CustomModlinksUri}\" from command." : "No modlinks were provided. Please try again",
                        success ? Icon.Success : Icon.Warning));
                }

                if (urlSchemeHandler.UrlSchemeCommand == UrlSchemeCommands.baseLink)
                {
                    bool success = false;
                    if (string.IsNullOrEmpty(urlSchemeHandler.Data))
                    {
                        Trace.TraceError($"{UrlSchemeCommands.baseLink}:{urlSchemeHandler.Data} not found");
                        success = false;
                    }
                    else
                    {
                        settings.BaseLink = urlSchemeHandler.Data;
                        success = true;
                    }

                    Dispatcher.UIThread.InvokeAsync(async () => await urlSchemeHandler.ShowConfirmation(
                            title: "Use new baselink from command",
                            message: success ? $"Got the base link \"{settings.BaseLink}\" from command." : "No baselink was provided. Please try again",
                            success ? Icon.Success : Icon.Warning));
                    
                }
            }
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

            const int Timeout = 10_000;
            const string LatestReleaseLinkJson =
                "https://raw.githubusercontent.com/TheMulhima/Scarab/static-resources/UpdateLinks.json";

            string json;
            string updateLink;
            
            try
            {
                var hc = new HttpClient();
                
                hc.DefaultRequestHeaders.Add("User-Agent", "Scarab");
                CancellationTokenSource cts = new CancellationTokenSource(Timeout);
                var links = await hc.GetStringAsync(new Uri(LatestReleaseLinkJson), cts.Token);
                
                JsonDocument linksDoc = JsonDocument.Parse(links);
                if (!linksDoc.RootElement.TryGetProperty("latestRelease", out JsonElement latestReleaseLinkElem)) 
                    return;
                if (!linksDoc.RootElement.TryGetProperty("updateLink", out JsonElement updateLinkElem)) 
                    return;
                
                string? latestReleaseLink = latestReleaseLinkElem.GetString();
                string? _updateLink = updateLinkElem.GetString();

                if (latestReleaseLink is null || _updateLink is null)
                    return;
                
                cts = new CancellationTokenSource(Timeout);
                json = await hc.GetStringAsync(new Uri(latestReleaseLink), cts.Token);
                updateLink = _updateLink;

            }
            catch (Exception) {
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
                Process.Start(new ProcessStartInfo(updateLink) { UseShellExecute = true });
                
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
            Trace.WriteLine("Loaded app");
            ((IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime)!.ShutdownRequested +=
                (_, _) => Program.CloseTraceFile();
        }

        public void LoadApp() => Dispatcher.UIThread.InvokeAsync(async () =>
        {
            Loading = true;
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

            Loading = false;
            if (isFirstLoad) isFirstLoad = false;
        });
    }
}
