using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Diagnostics;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Scarab.Extensions;
using Scarab.Models;
using Scarab.ViewModels;

namespace Scarab.Views.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (OperatingSystem.IsMacOS())
            {
                MacSpacer.IsVisible = true;
                NonMacSpacer.IsVisible = false;
                Title.Margin = new Thickness(5,10,0,0);
            }
        }

        private void OnTabSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            foreach (var element in (sender as TabStrip).GetLogicalDescendants())
            {
                if (element is not TabStripItem { Content: SelectableItem<ViewModelBase> content } tabStripItem) 
                    continue;

                var indicator = tabStripItem.GetFirstChild<StackPanel>().GetFirstChild<Rectangle>();
                if (indicator == null) continue;

                if (e.AddedItems.Count > 0 && content.DisplayName == (e.AddedItems[0] as SelectableItem<ViewModelBase>)?.DisplayName)
                {
                    indicator.Fill = Application.Current?.Resources["HighlightBlue"] as IBrush;
                }
                if (e.RemovedItems.Count > 0 && content.DisplayName == (e.RemovedItems[0] as SelectableItem<ViewModelBase>)?.DisplayName)
                {
                    indicator.Fill = Brushes.Transparent;
                }
            }
        }
    }
}
