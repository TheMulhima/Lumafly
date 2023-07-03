using Scarab.Models;
using System.Collections.Generic;
using System.Linq;
using PropertyChanged.SourceGenerator;

namespace Scarab.ViewModels
{
    public partial class UninstallDependenciesConfirmationWindowViewModel : ViewModelBase
    {
        public UninstallDependenciesConfirmationWindowViewModel(List<SelectableItem<ModItem>> options, bool externalModsInstalled)
        {
            Options = options;
            OptionsList = string.Join(", ", options.Select(x => x.Item.Name));

            ExternalModsInstalled = externalModsInstalled;
        }

        [Notify] 
        private List<SelectableItem<ModItem>> _options = new ();
        public string OptionsList { get; }
        public bool ExternalModsInstalled { get; }

        public void ToggleAll(object value)
        {
            foreach (var option in Options)
            {
                option.IsSelected = bool.Parse((string)value);
            }
        }
    }
}
