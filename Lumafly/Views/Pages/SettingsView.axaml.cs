using Avalonia.Markup.Xaml;
using Lumafly.ViewModels;

namespace Lumafly.Views.Pages
{
    public partial class SettingsView : View<SettingsViewModel>
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
