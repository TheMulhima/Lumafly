using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Lumafly.Util;
using Lumafly.Models;
using Lumafly.ViewModels;

namespace Lumafly.Views.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        if (OperatingSystem.IsMacOS())
        {
            MacSpacer.IsVisible = true;
            NonMacSpacer.IsVisible = false;
            AppName.Margin = new Thickness(5,10,0,0);
            AppVersion.Margin = new Thickness(1,15,0,-5);
        }
    }
}
