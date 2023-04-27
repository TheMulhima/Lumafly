using Avalonia;
using Avalonia.Controls;
using Avalonia.Diagnostics;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using JetBrains.Annotations;
using ReactiveUI;

namespace Scarab.CustomControls;

public class ErrorPopup : Window
{
    public ErrorPopup()
    {
        InitializeComponent();

        this.FindControl<Button>("OkButton").Command = ReactiveCommand.Create(Close);
    }

    private void InitializeComponent(bool loadXaml = true, bool attachDevTools = true)
    {
        #if DEBUG
        if (attachDevTools)
        {
            this.AttachDevTools(new DevToolsOptions
            {
                Size = new Size(640, 480)
            }); // press f12 to open
        }
        #endif
        if (loadXaml)
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}