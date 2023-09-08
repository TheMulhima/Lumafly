using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Scarab.ViewModels;

namespace Scarab.Views.Pages;

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
        
        PackManagerViewModel.OnPackLoaded += () => _notify?.Show(
            new Notification("Success", 
                "Pack Loaded Successfully",
            NotificationType.Success));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var topLevel = TopLevel.GetTopLevel(this);

        _notify = new WindowNotificationManager(topLevel)
        {
            MaxItems = 1
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}