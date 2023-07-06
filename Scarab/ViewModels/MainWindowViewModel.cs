using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using JetBrains.Annotations;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using Microsoft.Extensions.DependencyInjection;
using Scarab.Interfaces;
using Scarab.Models;
using Scarab.Services;
using Scarab.Util;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using PropertyChanged.SourceGenerator;
using Scarab.Enums;
using FileSystem = System.IO.Abstractions.FileSystem;

namespace Scarab.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        internal static bool _Debug
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
        
        [Notify]
        private LoadingViewModel _loadingPage { get; set; }

        [UsedImplicitly]
        private ViewModelBase Content => Loading || SelectedTabIndex < 0 ? LoadingPage : Tabs[SelectedTabIndex].Item;
        public IBrush BorderBrush => new SolidColorBrush(Color.FromRgb(0x28, 0x28, 0x28));
        public Thickness BorderThickness => new(1);
        public CornerRadius CornerRadius => new(3);
        public string AppName => $"Scarab+";
        public string AppVersion => $"v{Assembly.GetExecutingAssembly().GetName().Version}";

        [Notify]
        private ObservableCollection<SelectableItem<ViewModelBase>> _tabs = new ();

        [Notify]
        private int _selectedTabIndex = -1;

        [Notify]
        private bool _loading = true;
        
        public event Action? OnSelectTab;

        /// <summary>
        /// The main function that loads the data the app needs and sets up all the services
        /// </summary>
        /// <param name="initialTab">The index of the tab that is shown after load is finished</param>
        private async Task Impl(int initialTab)
        {
            LoadingPage = new LoadingViewModel();
            
            Trace.WriteLine($"Opening Scarab Version: {Assembly.GetExecutingAssembly().GetName().Version}");
            
            var urlSchemeHandler = new UrlSchemeHandler(handled: !isFirstLoad);

            await HandleURLSchemeCommand(urlSchemeHandler);

            Trace.WriteLine("Checking if up to date...");
            
            await Updater.CheckUpToDate();
            
            var sc = new ServiceCollection();
            var fs = new FileSystem();

            Trace.WriteLine("Loading settings.");

            HandleResetUrlScheme(urlSchemeHandler);
            HandleResetAllGlobalSettingsUrlScheme(urlSchemeHandler);

            Settings settings = Settings.Load() ?? Settings.Create(await GetSettingsPath());

            if (settings.PreferredLanguage == null)
            {
                settings.PreferredLanguage = 
                    Enum.TryParse(Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName, out SupportedLanguages preferredLanguage) 
                    ? preferredLanguage 
                    : SupportedLanguages.en;
                settings.Save();
            }

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
                        ).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());
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
                    ).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());
                    throw;
                }

                var offlineMode = await MessageBoxUtil.GetMessageBoxStandardWindow
                (
                    title: Resources.MVVM_UnableToGetModlinks,
                    text: string.Format(Resources.MWVM_Impl_Error_Fetch_ModLinks_Msgbox_Text, failedOp) + "\n\n" +
                          $"{Resources.MVVM_LaunchOfflineMode}",
                    icon: Icon.Warning,
                    @enum: ButtonEnum.YesNo
                ).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow()) == ButtonResult.Yes;

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
            SelectedTabIndex = initialTab;
            OnSelectTab?.Invoke();
            Trace.WriteLine("Selected Tab 0");
        }

        private async Task HandleURLSchemeCommand(IUrlSchemeHandler urlSchemeHandler)
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length == 2) // only accept 2 args, the exe location and the uri 
            {
                urlSchemeHandler.SetCommand(args[1]);
            }

            if (!urlSchemeHandler.Handled && urlSchemeHandler.UrlSchemeCommand != UrlSchemeCommands.none)
            {
                var prompt = urlSchemeHandler.UrlSchemeCommand switch
                {
                    UrlSchemeCommands.download                    => $"Download the following mods: {GetListOfMods()}",
                    UrlSchemeCommands.reset                       => $"Reset Scarab's persistent settings",
                    UrlSchemeCommands.forceUpdateAll              => $"Reinstall all mods which could help fix issues that happened because mods are not downloaded correctly.",
                    UrlSchemeCommands.customModLinks              => $"Load a custom mod list from: {urlSchemeHandler.Data}",
                    UrlSchemeCommands.baseLink                    => $"Load Modlinks and APILinks from: {urlSchemeHandler.Data}",
                    UrlSchemeCommands.removeAllModsGlobalSettings => $"Reset all mods' global settings",
                    UrlSchemeCommands.removeGlobalSettings        => $"Remove global settings for the following mods: {GetListOfMods()}",
                    _ => ""
                };
                bool accepted = await LoadingPage.ShowUrlSchemePrompt(prompt);

                if (!accepted) urlSchemeHandler.SetCommand(UrlSchemeCommands.none);
            }
            
            
            string GetListOfMods()
            {
                var mods = urlSchemeHandler.ParseDownloadCommand(urlSchemeHandler.Data);
                if (mods.Count == 0) return "None";
                var list = "\n\n";
                foreach (var (mod, url) in mods)
                {
                    list += mod;
                    if (url is not null)
                        list += $" ({Resources.MVVM_NotInModlinks_Disclaimer}),\n";
                    else
                        list += ",\n";
                }

                return list;
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
                    var di = new DirectoryInfo(Settings.GetOrCreateDirPath());

                    foreach (var file in di.GetFiles())
                    {
                        // save all log files
                        if (file.Name.EndsWith(Program.LoggingFileExtension)) continue;
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
                    title: Resources.MVVM_ResetUrlScheme_Header,
                    message: success ? Resources.MVVM_ResetUrlScheme_Body_Success : string.Format(Resources.MVVM_ResetUrlScheme_Body_Failure, exception),
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
                    title: Resources.MVVM_RemoveAllGlobalSettingUrlScheme_Header,
                    message: success 
                        ? Resources.MVVM_RemoveAllGlobalSettingUrlScheme_Body_Success 
                        : string.Format(Resources.MVVM_RemoveAllGlobalSettingUrlScheme_Body_Failure, exception),
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
                        title: Resources.MVVM_LoadCustomModlinksUrlScheme_Header, 
                        message: success ? string.Format(Resources.MVVM_LoadCustomModlinksUrlScheme_Body_Success, settings.CustomModlinksUri) : Resources.MVVM_LoadCustomModlinksUrlScheme_Body_Failure,
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
                            title: Resources.MVVM_LoadCustomBaseLinkUrlScheme_Header,
                            message: success ? string.Format(Resources.MVVM_LoadCustomBaseLinkUrlScheme_Body_Success, settings.BaseLink) : Resources.MVVM_LoadCustomBaseLinkUrlScheme_Body_Failure,
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
                        ).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError($"Error happened when trying to find out if config files are locked. {e}");
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
            ).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());

            return Settings.Create(await GetSettingsPath());
        }

        private static async Task<string> GetSettingsPath()
        {
            if (!Settings.TryAutoDetect(out ValidPath? path))
            {
                var info = MessageBoxUtil.GetMessageBoxStandardWindow
                (
                    new MessageBoxStandardParams
                    {
                        ContentHeader = Resources.MWVM_Info,
                        ContentMessage = Resources.MWVM_UnableToDetect_Message,
                        MinWidth = 550
                    }
                );

                await info.ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());
                
                return await PathUtil.SelectPath();
            }

            Trace.WriteLine($"Settings doesn't exist. Creating it at detected path {path}.");

            var window = MessageBoxUtil.GetMessageBoxStandardWindow
            (
                new MessageBoxStandardParams
                {
                    ContentHeader = Resources.MWVM_DetectedPath_Title,
                    ContentMessage = string.Format(Resources.MWVM_DetectedPath_Message, path.Root),
                    ButtonDefinitions = ButtonEnum.YesNo
                }
            );

            ButtonResult res = await window.ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());

            return res == ButtonResult.Yes
                ? Path.Combine(path.Root, path.Suffix)
                : await PathUtil.SelectPath();
        }
        
        public MainWindowViewModel()
        {
            Instance = this;
            _loadingPage = new LoadingViewModel();
            LoadApp();
            Trace.WriteLine("Loaded app");
            ((IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime)!.ShutdownRequested +=
                (_, _) => Program.CloseTraceFile();
        }

        public void LoadApp(int initialTab = 0) => Dispatcher.UIThread.InvokeAsync(async () =>
        {
            Loading = true;
            try
            {
                await Impl(initialTab);
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
