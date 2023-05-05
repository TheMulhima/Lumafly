using Avalonia.Markup.Xaml;
using JetBrains.Annotations;
using Scarab.ViewModels;

namespace Scarab.Views
{
    [UsedImplicitly]
    public class ProfileManagerView: View<ProfileManagerViewModel>
    {
        public ProfileManagerView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
