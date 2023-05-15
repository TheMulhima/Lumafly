using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using Scarab.Interfaces;
using Scarab.Util;

namespace Scarab.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly ISettings _settings;
        private readonly IModSource _mods;
        private bool useCustomModlinksOriginalValue;
        private string pathOriginalValue;
        
        public ReactiveCommand<Unit, Unit> ChangePath { get; }

        public SettingsViewModel(ISettings settings, IModSource mods)
        {
            _settings = settings;
            _mods = mods;
            
            ChangePath = ReactiveCommand.CreateFromTask(ChangePathAsync);

            useCustomModlinksOriginalValue = _settings.UseCustomModlinks;
            pathOriginalValue = _settings.ManagedFolder;
            _customModlinksUri = _settings.CustomModlinksUri;
            ((IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime)!.ShutdownRequested +=
                SaveCustomModlinksUri;
        }

        private void SaveCustomModlinksUri(object? sender, ShutdownRequestedEventArgs e)
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
            }
        }

        private ObservableCollection<string> AutoRemoveDepsOptions => new (Enum.GetNames<AutoRemoveUnusedDepsOptions>());

        private string AutoRemoveDepSelection
        {
            get => _settings.AutoRemoveUnusedDeps.ToString();
            set
            {
                _settings.AutoRemoveUnusedDeps = Enum.Parse<AutoRemoveUnusedDepsOptions>(value);
                _settings.Save();
            }
        }
        
        private bool UseCustomModlinks
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
        
        private string CurrentPath => _settings.ManagedFolder.Replace(@"\\", @"\");

        private string _customModlinksUri;

        private string CustomModlinksUri
        {
            get => _customModlinksUri;
            set
            {
                _customModlinksUri = value;
                RaisePropertyChanged(nameof(AskForReload));
            }
        }

        private bool AskForReload => CustomModlinksUri != _settings.CustomModlinksUri ||
                                     useCustomModlinksOriginalValue != _settings.UseCustomModlinks ||
                                     pathOriginalValue != _settings.ManagedFolder; 
        
        private void ReloadApp()
        {
            _settings.CustomModlinksUri = CustomModlinksUri;
            _settings.Save();
            MainWindowViewModel.Instance?.LoadApp();
        }

        private async Task ChangePathAsync()
        {
            string? path = await PathUtil.SelectPathFallible();

            if (path is null)
                return;

            _settings.ManagedFolder = path;
            _settings.Save();

            await _mods.Reset();
            
            RaisePropertyChanged(nameof(AskForReload));
        }
    }
}
