using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;

namespace Scarab.CustomControls;

public partial class ErrorPopup : Window
{
    public ErrorPopup()
    {
        InitializeComponent();

        this.FindControl<Button>("OkButton").Command = ReactiveCommand.Create(Close);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
}