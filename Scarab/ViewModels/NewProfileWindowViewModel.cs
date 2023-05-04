using Avalonia.Controls;
using Avalonia.Interactivity;
using DynamicData;
using JetBrains.Annotations;
using ReactiveUI;
using Scarab.Interfaces;
using Scarab.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace Scarab.ViewModels
{
    public class NewProfileWindowViewModel : ViewModelBase
    {
        public string? ProfileName { get; set; }

        private readonly SourceList<string> _allModNames;

        [UsedImplicitly]
        public string[] AllModNames => _allModNames.Items.ToArray();

        private ObservableCollection<string> _selectedModNames;
        public ObservableCollection<string> SelectedModNames
        {
            get => _selectedModNames;
            set => this.RaiseAndSetIfChanged(ref _selectedModNames, value);
        }

        public NewProfileWindowViewModel(IModDatabase db)
        {
            _allModNames = new SourceList<string>();
            _allModNames.AddRange(db.Items.Select(modItem => modItem.Name));

            _selectedModNames = new ObservableCollection<string>();
        }

        [UsedImplicitly]
        public void RemoveMod(object? sender, RoutedEventArgs routedEventArgs)
        {
            var textBlock = ((Button)sender!).Parent?.VisualChildren.FirstOrDefault(child => child is TextBlock);
            var modName = ((TextBlock)textBlock!).Text;
            if (modName is not null)
            {
                SelectedModNames.Remove(modName);
            }
        }

        [UsedImplicitly]
        public void CreateProfile()
        {
            ProfileManager.CreateProfile(ProfileName, SelectedModNames.ToArray());
        }
    }
}
