using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Lumafly.Views.Pages;

public partial class InfoView : UserControl
{
    public InfoView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}