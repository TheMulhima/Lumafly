using System.ComponentModel;

namespace Scarab.Models
{
    public record SelectableItem<T>(T Item, string DisplayName, bool IsSelected) : INotifyPropertyChanged
    {
        private bool _isSelected = IsSelected;

        public bool IsSelected 
        { 
            get => _isSelected; 
            set
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }

        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}