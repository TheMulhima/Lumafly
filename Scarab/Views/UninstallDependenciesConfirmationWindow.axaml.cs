using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Scarab.ViewModels;

namespace Scarab.Views
{
    public partial class UninstallDependenciesConfirmationWindow : Window
    {
        public UninstallDependenciesConfirmationWindow()
        {
            InitializeComponent();
        }

        public UninstallDependenciesConfirmationWindow(UninstallDependenciesViewModel context)
        {
            InitializeComponent();
            DataContext = context;
            context.CloseAction = Close;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
