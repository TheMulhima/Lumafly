using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Diagnostics;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Scarab.Models;

namespace Scarab.Views
{
    public partial class MainWindow : Window
    {
        private TabStrip _tabs;
        public MainWindow()
        {
            InitializeComponent();

            if (OperatingSystem.IsMacOS())
            {
                this.FindControl<Rectangle>("MacSpacer").IsVisible = true;
                this.FindControl<Rectangle>("NonMacSpacer").IsVisible = false;
                this.FindControl<TextBlock>("Title").Margin = new Thickness(5,10,0,0);
            }

            _tabs = this.FindControl<TabStrip>("Tabs");
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

        private void OnTabSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            foreach (var element in (sender as TabStrip).GetLogicalDescendants())
            {
                if (element is not TabStripItem { Content: TabItemModel content } tabStripItem) 
                    continue;

                if (e.AddedItems.Count > 0 && content.Header == (e.AddedItems[0] as TabItemModel)?.Header)
                {
                    tabStripItem.Background = Application.Current?.Resources["HighlightBlue"] as IBrush;
                }
                if (e.RemovedItems.Count > 0 && content.Header == (e.RemovedItems[0] as TabItemModel)?.Header)
                {
                    tabStripItem.Background = Brushes.Transparent;
                }
            }
        }
    }
}
