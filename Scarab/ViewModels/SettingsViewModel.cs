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
        
        public ReactiveCommand<Unit, Unit> ChangePath { get; }

        public SettingsViewModel(ISettings settings, IModSource mods)
        {
            _settings = settings;
            _mods = mods;
            
            ChangePath = ReactiveCommand.CreateFromTask(ChangePathAsync);
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

        private string CurrentPath => _settings.ManagedFolder.Replace(@"\\", @"\");

        private void ReloadApp()
        {
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
            
            ReloadApp();
        }
    }
}
