using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Lumafly.ViewModels;

namespace Lumafly.Views.Pages;

public partial class InfoView : View<LoadingViewModel>
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