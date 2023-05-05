using Scarab.ViewModels;

namespace Scarab.Models
{
    public class TabItemModel
    {
        public string Header { get; }
        
        public ViewModelBase ViewModel { get; }

        public TabItemModel(string header, ViewModelBase viewModel)
        {
            Header = header;
            ViewModel = viewModel;
        }
    }
}
