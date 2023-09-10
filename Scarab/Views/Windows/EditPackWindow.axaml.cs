using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Scarab.Util;

namespace Scarab.Views.Windows;

public partial class EditPackWindow : Window
{
    public EditPackWindow()
    {
        InitializeComponent();

        var mainWindow = AvaloniaUtils.GetMainWindow();
        Width = mainWindow.Width;
        Height = mainWindow.Height;
    }

    private void RequestSave(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
    
    private void RequestCancel(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void DismissOnClick(object? sender, RoutedEventArgs e)
    {

        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            
            if (sender is not ILogical button) return;
            var stackPanel = button.GetLogicalParent();
            var flyoutPresenter = stackPanel?.GetLogicalParent();
            var popup = flyoutPresenter?.GetLogicalParent() as Popup;
            
            await Task.Delay(100);

            popup?.Close();
            
            await Task.Delay(100);
            
            popup?.Close();
        });
    }
}