using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;

namespace Scarab.Views.Windows
{
    public partial class UninstallDependenciesConfirmationWindow : Window
    {
        public UninstallDependenciesConfirmationWindow()
        {
            InitializeComponent();
            YesButton.Command = ReactiveCommand.Create(() => Close(true));
            NoButton.Command = ReactiveCommand.Create(() => Close(false));
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
