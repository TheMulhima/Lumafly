using Avalonia;
using Avalonia.Controls;
using Avalonia.Diagnostics;
using Avalonia.Markup.Xaml;

namespace Scarab.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent(bool loadXaml = true, bool attachDevTools = true)
        {
            #if DEBUG
            if (attachDevTools)
            {
                this.AttachDevTools(new DevToolsOptions
                {
                    Size = new Size(640, 480)
                }); // press f12 to open
            }
            #endif
            if (loadXaml)
            {
                AvaloniaXamlLoader.Load(this);
            }
        }
    }
}
