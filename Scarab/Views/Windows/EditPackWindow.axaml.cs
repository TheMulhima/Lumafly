using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Scarab.Views.Windows;

public partial class EditPackWindow : Window
{
    public EditPackWindow()
    {
        InitializeComponent();
    }

    private void RequestSave(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
    
    private void RequestCancel(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}