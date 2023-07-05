using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
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
    
    // apparently view model exists in arrange core
    protected override void ArrangeCore(Rect finalRect)
    {
        base.ArrangeCore(finalRect);
            
        ViewModel.OnSelectTab += OnTabSelectionChanges;
    }
    
    private MainWindowViewModel ViewModel => DataContext as MainWindowViewModel ?? throw new NullReferenceException();

    private void OnTabSelectionChanges()
    {
        Dispatcher.UIThread.Invoke(async () =>
        {
            await Task.Delay(100);
            foreach (var element in TabStrip.GetLogicalDescendants())
            {
                if (element is not TabStripItem { Content: SelectableItem<ViewModelBase> content } tabStripItem) 
                    continue;

                var indicator = tabStripItem.GetFirstChild<StackPanel>().GetFirstChild<Rectangle>();
                if (indicator == null) continue;
            
                if (ViewModel.SelectedTabIndex == ViewModel.Tabs.IndexOf(content))
                {
                    indicator.Fill = Application.Current?.Resources["HighlightBlue"] as IBrush;
                }
                else
                {
                    indicator.Fill = Brushes.Transparent;
                }
            }
        });
    }

    private void TabSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0) return;
        OnTabSelectionChanges();
    }
}
