using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using HarfBuzzSharp;
using ReactiveUI;
using Lumafly.Enums;
using Lumafly.Interfaces;
using Lumafly.Services;
using Lumafly.Util;

namespace Lumafly.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly ISettings _settings;
        private readonly IModSource _mods;
        private readonly IAppUpdater _updater;
        private bool useGithubMirrorOriginalValue;
        private bool useCustomModlinksOriginalValue;
        private bool cacheDownloadsOriginalValue;
        private string pathOriginalValue;
        
        public ReactiveCommand<Unit, Unit> ChangePath { get; }

        public SettingsViewModel(ISettings settings, IModSource mods, IAppUpdater updater)
        {
            Trace.WriteLine("Initializing SettingsViewModel");
            _settings = settings;
            _mods = mods;
            _updater = updater;
            
            ChangePath = ReactiveCommand.CreateFromTask(ChangePathAsync);

            useCustomModlinksOriginalValue = _settings.UseCustomModlinks;
            useGithubMirrorOriginalValue = _settings.UseGithubMirror;
            pathOriginalValue = _settings.ManagedFolder;
            _customModlinksUri = _settings.CustomModlinksUri;
            _githubMirrorFormat = _settings.GithubMirrorFormat;
            cacheDownloadsOriginalValue = _settings.LowStorageMode;
            ((IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime)!.ShutdownRequested +=
                SaveCustomModlinksUri;
            
            Trace.WriteLine("SettingsViewModel Initialized");
        }
        
        private readonly Dictionary<AutoRemoveUnusedDepsOptions, string> LocalizedAutoRemoveDepsOptions = new()
        {
            { AutoRemoveUnusedDepsOptions.Never, Resources.XAML_Off },
            { AutoRemoveUnusedDepsOptions.Ask, Resources.XAML_Ask },
            { AutoRemoveUnusedDepsOptions.Always, Resources.XAML_On }
        };

        public void SaveCustomModlinksUri(object? sender, ShutdownRequestedEventArgs e)
        {
            _settings.CustomModlinksUri = CustomModlinksUri;
            _settings.Save();
        }

        public bool WarnBeforeRemovingDependents
        {
            get => _settings.WarnBeforeRemovingDependents;
            set
            {
                _settings.WarnBeforeRemovingDependents = value;
                _settings.Save();
                RaisePropertyChanged(nameof(WarnBeforeRemovingDependents));
            }
        }

        public ObservableCollection<string> AutoRemoveDepsOptions => new (LocalizedAutoRemoveDepsOptions.Values);
        public ObservableCollection<string> LanguageOptions => new (SupportedLanguagesInfo.LocalizedLanguageOptions.Values);

        public string AutoRemoveDepSelection
        {
            get => LocalizedAutoRemoveDepsOptions[_settings.AutoRemoveUnusedDeps];
            set
            {
                _settings.AutoRemoveUnusedDeps = LocalizedAutoRemoveDepsOptions.First(x => x.Value == value).Key;
                _settings.Save();
            }
        }
        
        public string LanguageSelection
        {
            get => SupportedLanguagesInfo.LocalizedLanguageOptions[_settings.PreferredLanguage ?? SupportedLanguages.en];
            set
            {
                _settings.PreferredLanguage = SupportedLanguagesInfo.LocalizedLanguageOptions.First(x => x.Value == value).Key;
                _settings.Save();
                Thread.CurrentThread.CurrentUICulture =
                    new CultureInfo(SupportedLanguagesInfo.SupportedLangToCulture[_settings.PreferredLanguage ?? SupportedLanguages.en]);
                ReloadApp();
            }
        }
        
        public bool UseCustomModlinks
        {
            get => _settings.UseCustomModlinks;
            set
            {
                _settings.UseCustomModlinks = value;
                _settings.Save();
                RaisePropertyChanged(nameof(UseCustomModlinks));
                RaisePropertyChanged(nameof(AskForReload));
            }
        }

        public bool UseGithubMirror
        {
            get => _settings.UseGithubMirror;
            set
            {
                _settings.UseGithubMirror = value;
                _settings.Save();
                RaisePropertyChanged(nameof(UseGithubMirror));
                RaisePropertyChanged(nameof(AskForReload));
            }
        }

        public bool LowStorageMode
        {
            get => _settings.LowStorageMode;
            set
            {
                _settings.LowStorageMode = value;
                _settings.Save();
                RaisePropertyChanged(nameof(LowStorageMode));
                RaisePropertyChanged(nameof(AskForReload));
            }
        }

        public string ExtraSpaceTaken => string.Format(Resources.XAML_CacheDownloads_Explanation, _settings.ExtraSpaceTaken);
        
        public string CurrentPath => _settings.ManagedFolder.Replace(@"\\", @"\");

        private string _customModlinksUri;

        public string CustomModlinksUri
        {
            get => _customModlinksUri;
            set
            {
                _customModlinksUri = value;
                RaisePropertyChanged(nameof(AskForReload));
                
                // throw errors if the URI is invalid to make the text box red
                // Although it wont actually stop anything cuz in the end if its an invalid URI
                // on reload it will show popup and just be set to empty
                try { _ = new Uri(value); }
                catch (UriFormatException) { throw new ArgumentException("Invalid URI"); }
            }
        }

        private string _githubMirrorFormat;

        public string GithubMirrorFormat
        {
            get => _githubMirrorFormat;
            set
            {
                _githubMirrorFormat = value;
                RaisePropertyChanged(nameof(AskForReload));
            }
        }

        public bool AskForReload =>  CustomModlinksUri != _settings.CustomModlinksUri ||
                                     GithubMirrorFormat != _settings.GithubMirrorFormat ||
                                     useGithubMirrorOriginalValue != _settings.UseGithubMirror ||
                                     useCustomModlinksOriginalValue != _settings.UseCustomModlinks ||
                                     cacheDownloadsOriginalValue != _settings.LowStorageMode ||
                                     pathOriginalValue != _settings.ManagedFolder;

        public string[] YesNo => new[] { "Yes", "No" };
        
        public void ReloadApp()
        {
            _settings.GithubMirrorFormat = GithubMirrorFormat;
            _settings.CustomModlinksUri = CustomModlinksUri;
            _settings.Save(); 
            Dispatcher.UIThread.InvokeAsync(async () => await MainWindowViewModel.Instance!.LoadApp(3));
        }

        public async Task ChangePathAsync()
        {
            string? path = await PathUtil.SelectPathFallible();

            if (path is null)
                return;

            _settings.ManagedFolder = path;
            _settings.Save();

            await _mods.Reset();
            
            RaisePropertyChanged(nameof(AskForReload));
        }

        public async Task CheckForUpdates()
        {
            await _updater.CheckUpToDate(forced: true);
        }
        public void OpenLogsFolder()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Settings.GetOrCreateDirPath(),
                UseShellExecute = true,
            });
        }
        
        public void Donate() => Process.Start(new ProcessStartInfo("https://ko-fi.com/mulhima") { UseShellExecute = true });

    }
}
