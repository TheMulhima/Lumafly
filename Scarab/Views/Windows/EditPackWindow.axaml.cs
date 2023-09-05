using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MessageBox.Avalonia.Enums;
using MsBox.Avalonia.Enums;
using Scarab.Util;
using Scarab.ViewModels;

namespace Scarab.Views.Windows;

public partial class EditPackWindow : Window
{
    public EditPackWindow()
    {
        InitializeComponent();
    }

    private void RequestSave(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
    
    private void RequestCancel(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}