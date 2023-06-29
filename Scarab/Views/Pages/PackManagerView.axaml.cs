using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Scarab.ViewModels;

namespace Scarab.Views.Pages;

public partial class PackManagerView : View<PackManagerViewModel>
{
    public PackManagerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}