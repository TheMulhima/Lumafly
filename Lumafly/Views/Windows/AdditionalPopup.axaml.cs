using Avalonia;
using Avalonia.Controls;
using Avalonia.Diagnostics;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using ReactiveUI;

namespace Lumafly.Views.Windows;

public partial class AdditionalPopup : Window
{
    public AdditionalPopup()
    {
        InitializeComponent();

        OkButton.Command = ReactiveCommand.Create(Close);
    }
}