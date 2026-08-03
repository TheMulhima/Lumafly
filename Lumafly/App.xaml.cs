using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using JetBrains.Annotations;
using Lumafly.Util;
using Lumafly.ViewModels;
using Lumafly.Views.Windows;
using ReactiveUI;
using System;
using System.Reactive;

namespace Lumafly
{
    [UsedImplicitly]
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex =>
            {
                _ = DisplayErrors.DisplayGenericError(ex.Message, ex);
            });
            Dispatcher.UIThread.UnhandledException += (_, e) =>
            {
                _ = DisplayErrors.DisplayGenericError("Unhandled error!", e.Exception);
                e.Handled = true;
            };

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
