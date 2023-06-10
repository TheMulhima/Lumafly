using Avalonia;
using Avalonia.Controls;
using Avalonia.Diagnostics;
using Avalonia.Markup.Xaml;
using ReactiveUI;

namespace Scarab.Views.Windows;

public partial class ErrorPopup : Window
{
    public ErrorPopup()
    {
        InitializeComponent();

        OkButton.Command = ReactiveCommand.Create(Close);
        CopyButton.Command = ReactiveCommand.Create(
            () => Clipboard?.SetTextAsync(ErrorExplanation.Text + "\n\n" + FullErrorText.Text));
    }
}