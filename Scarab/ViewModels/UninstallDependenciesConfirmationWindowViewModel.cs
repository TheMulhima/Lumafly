using Scarab.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Scarab.ViewModels
{
    public class UninstallDependenciesConfirmationWindowViewModel : ViewModelBase
    {
        public UninstallDependenciesConfirmationWindowViewModel(List<SelectableItem<ModItem>> options, bool externalModsInstalled)
        {
            Options = new ObservableCollection<SelectableItem<ModItem>>(options);
            OptionsList = string.Join(", ", options.Select(x => x.Item.Name));

            ExternalModsInstalled = externalModsInstalled;
        }

        public ObservableCollection<SelectableItem<ModItem>> Options { get; }
        public string OptionsList { get; }
        public bool ExternalModsInstalled { get; }

        public void ToggleAll(bool value)
        {
            foreach (var option in Options)
            {
                option.IsSelected = value;
            }
        }
    }
}
