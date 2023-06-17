using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Scarab.Util;
using Scarab.Models;
using Scarab.ViewModels;

namespace Scarab.Views.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        if (OperatingSystem.IsMacOS())
        {
            MacSpacer.IsVisible = true;
            NonMacSpacer.IsVisible = false;
            AppName.Margin = new Thickness(5,10,0,0);
            AppVersion.Margin = new Thickness(1,15,0,-5);
        }
    }

    private void OnTabSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0) return;
        if (sender is not TabStrip tabStrip) return;
        foreach (var element in tabStrip.GetLogicalDescendants())
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
