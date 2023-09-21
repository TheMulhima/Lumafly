using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Lumafly.Util;

namespace Lumafly.Views.Windows;

public partial class ReadmePopup : Window
{
    public ReadmePopup()
    {
        InitializeComponent();

        var mainWindow = AvaloniaUtils.GetMainWindow();
        Width = mainWindow.Width;
        Height = mainWindow.Height;
    }

    private void Close(object? sender, RoutedEventArgs e) => Close();
}