using Avalonia.Controls;

namespace Lumafly.Views
{
    public class View<T> : UserControl where T : class
    {
        public new T DataContext { get; set; } = null!;
    }
}