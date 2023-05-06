using Scarab.ViewModels;
using PropertyChanged.SourceGenerator;
using System.ComponentModel;

namespace Scarab.Models
{
    public partial class ModSelect : INotifyPropertyChanged
    {
        public ModItem Item { get; set; }

        [Notify]
        private bool _isSelected;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}