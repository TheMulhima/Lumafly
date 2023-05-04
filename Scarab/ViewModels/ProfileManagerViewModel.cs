using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using JetBrains.Annotations;
using ReactiveUI;
using Scarab.Interfaces;
using Scarab.Models;
using Scarab.Services;
using Scarab.Util;
using Scarab.Views;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;

namespace Scarab.ViewModels
{
    public class ProfileManagerViewModel : ViewModelBase
    {
        private IModDatabase _modDatabase;

        private SortableObservableCollection<Profile> _items;
        
        private readonly ISettings _settings;

        public Profile? SelectedProfile
        {
            get => Profile.CurrentProfile;
            set => ProfileManager.SetCurrentProfile(value);
        }

        [UsedImplicitly]
        internal SortableObservableCollection<Profile> Items
        {
            get => _items;
            set => this.RaiseAndSetIfChanged(ref _items, value);
        }
        
        public ReactiveCommand<Profile, Unit> Delete { get; }

        public ProfileManagerViewModel(IModDatabase modDatabase, ISettings settings)
        {
            _modDatabase = modDatabase;
            _settings = settings;
            _settings.Profiles.CollectionChanged += OnProfilesChanged;
            _items = new SortableObservableCollection<Profile>(_settings.Profiles.OrderBy(p => p.Current ? -1 : 1));
            Delete = ReactiveCommand.Create<Profile>(DeleteCommand);
        }

        private void OnProfilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Items = new SortableObservableCollection<Profile>(_settings.Profiles.OrderBy(p => p.Current ? -1 : 1));
        }

        private void DeleteCommand(Profile profile)
        {
            ProfileManager.RemoveProfile(profile);
        }
        
        public void OpenNewProfileWindow()
        {
            var newProfileWindow = new NewProfileWindow(_modDatabase);
            
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                newProfileWindow.ShowDialog(desktop.MainWindow);
            }
        }
    }
}
