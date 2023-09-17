using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ReactiveUI;

namespace Lumafly.Views.Windows
{
    public partial class UninstallDependenciesConfirmationWindow : Window
    {
        public UninstallDependenciesConfirmationWindow()
        {
            InitializeComponent();
        }
        
        public void Close_Yes(object sender, RoutedEventArgs args) => Close(true);
        public void Close_No(object sender, RoutedEventArgs args) => Close(false);

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
