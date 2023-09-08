using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Scarab.Views.Windows;

public partial class ImportPackPopup : Window
{
    public ImportPackPopup()
    {
        InitializeComponent();
    }

    private void Confirm(object? sender, RoutedEventArgs e)
    {
        Close(SharingCode.Text);
    }
    private void Cancel(object? sender, RoutedEventArgs e)
    {
        Close("");
    }
}