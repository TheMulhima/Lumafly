using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Scarab.Views;

public partial class SearchOptionsMenu : UserControl
{
    public SearchOptionsMenu()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}