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
            this.FindControl<Button>("YesButton").Command = ReactiveCommand.Create(() => Close(true));
            this.FindControl<Button>("NoButton").Command = ReactiveCommand.Create(() => Close(false));
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
