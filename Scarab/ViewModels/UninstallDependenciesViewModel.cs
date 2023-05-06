using Scarab.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Scarab.ViewModels
{
    public class UninstallDependenciesViewModel : ViewModelBase
    {
        public UninstallDependenciesViewModel(List<ModSelect> options, bool externalModsInstalled)
        {
            Options = new ObservableCollection<ModSelect>(options);
            OptionsList = string.Join(", ", options.Select(x => x.Item.Name));

            ExternalModsInstalled = externalModsInstalled;
        }

        public Action CloseAction { get; set; } = null!;
        public ObservableCollection<ModSelect> Options { get; }
        public string OptionsList { get; }
        public bool Result { get; private set; }
        public bool ExternalModsInstalled { get; }

        public void ToggleAll(bool value)
        {
            foreach (var option in Options)
            {
                option.IsSelected = value;
            }
        }

        public void SetResult(bool result)
        {
            Result = result;
            CloseAction();
        }
    }
}
