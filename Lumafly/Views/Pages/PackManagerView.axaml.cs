using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Lumafly.ViewModels;

namespace Lumafly.Views.Pages;

public partial class PackManagerView : View<PackManagerViewModel>
{
    private WindowNotificationManager? _notify;
    
    private PackManagerViewModel PackManagerViewModel => (((StyledElement)this).DataContext as PackManagerViewModel)!;

    public PackManagerView()
    {
        InitializeComponent();
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        base.ArrangeCore(finalRect);
        
        PackManagerViewModel.OnPackLoaded += (packName) => _notify?.Show(
            new Notification("Pack Loaded", 
                $"Pack {packName} has loaded Successfully",
            NotificationType.Success));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var topLevel = TopLevel.GetTopLevel(this);

        _notify = new WindowNotificationManager(topLevel)
        {
            MaxItems = 1,
            Margin = new Thickness(0, 50)
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}