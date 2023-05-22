using System;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Metadata;
using Scarab.Extensions;

namespace Scarab.Views.Controls;

/// <summary>
/// Creates a button with an attached flyout.
/// Only supports PlacementMode.Bottom and PlacementMode.Right.
/// </summary>
public class TextButtonFlyout : TemplatedControl
{
    private readonly Stopwatch lastOpenedStopwatch = new();
    private PathIcon? Icon;
    private Button? Button;

    private static StreamGeometry? ChevronDown => Application.Current?.Resources["chevron_down_regular"] as StreamGeometry;
    private static StreamGeometry? ChevronUp => Application.Current?.Resources["chevron_up_regular"] as StreamGeometry;
    private static StreamGeometry? ChevronLeft => Application.Current?.Resources["chevron_left_regular"] as StreamGeometry;
    private static StreamGeometry? ChevronRight => Application.Current?.Resources["chevron_right_regular"] as StreamGeometry;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Icon = e.NameScope.Find<PathIcon>("Icon");
        Button = e.NameScope.Find<Button>("Button");

        var flyout = new Flyout
        {
            Placement = FlyoutPlacement,
            ShowMode = FlyoutShowMode,
            Content = Content,
            HorizontalOffset = HorizontalOffset
        };
        
        flyout.Opened += OnFlyoutOpened;
        flyout.Closing += OnFlyoutClosing;
        flyout.Closed += OnFlyoutClosed;

        Button.Flyout = flyout;

        Background ??= Brushes.Transparent;
        OnHoverColor ??= Background;
        
        // it needs to be done like this because :pointerover doesnt accept bindings and
        // so the only option is to change the :pointerover setters directly
        Button.SetStyleSetterByName("Button:pointerover", BackgroundProperty, OnHoverColor);
        SetIcon();
    }

    private void OnFlyoutOpened(object? sender, EventArgs e)
    {
        if (lastOpenedStopwatch.IsRunning)
        {
            lastOpenedStopwatch.Stop();
            var lastClosedTime = lastOpenedStopwatch.ElapsedMilliseconds;
            lastOpenedStopwatch.Reset();

            // 300 ms is arbitrary cutoff point. testing shows attached flyouts are closed in ~150 ms
            // while non attached flyouts are closed in ~10 ms (difference is there due to checking in
            // pointer pressed event). 300 ms is safe enough for both cases.
            if (lastClosedTime < 300)
            {
                ((Flyout)sender!).Hide();
            }
        }
        
        SetIcon();
    }
    
    private void OnFlyoutClosing(object? sender, CancelEventArgs e)
    {
        lastOpenedStopwatch.Start();
    }
    private void OnFlyoutClosed(object? sender, EventArgs e)
    {
        SetIcon();
    }
    
    private void SetIcon()
    {
        Icon = Icon ?? throw new Exception("Flyout Button doesnt have icon");
        Button = Button ?? throw new Exception("Flyout Button doesnt have button");
        Icon.Data = Button.Flyout.IsOpen ? GetCloseSymbol() : GetOpenSymbol();
    }

    private StreamGeometry? GetOpenSymbol() => FlyoutPlacement switch
    {
        PlacementMode.Bottom or
            PlacementMode.BottomEdgeAlignedLeft or
            PlacementMode.BottomEdgeAlignedRight => ChevronDown,

        PlacementMode.Right or
            PlacementMode.RightEdgeAlignedBottom or
            PlacementMode.RightEdgeAlignedTop => ChevronRight,

        _ => throw new NotImplementedException()
    };

    private StreamGeometry? GetCloseSymbol() => FlyoutPlacement switch
    {
        PlacementMode.Bottom or
            PlacementMode.BottomEdgeAlignedLeft or
            PlacementMode.BottomEdgeAlignedRight => ChevronUp,

        PlacementMode.Right or
            PlacementMode.RightEdgeAlignedBottom or
            PlacementMode.RightEdgeAlignedTop => ChevronLeft,

        _ => throw new NotImplementedException()
    };


    #region FlyoutPlacement
    private PlacementMode _flyoutPlacement;

    public static readonly DirectProperty<TextButtonFlyout, PlacementMode> FlyoutPlacementProperty = AvaloniaProperty.RegisterDirect<TextButtonFlyout, PlacementMode>(
        "FlyoutPlacement", o => o.FlyoutPlacement, (o, v) => o.FlyoutPlacement = v, PlacementMode.Bottom);

    public PlacementMode FlyoutPlacement
    {
        get => _flyoutPlacement;
        set => SetAndRaise(FlyoutPlacementProperty, ref _flyoutPlacement, value);
    }
    #endregion

    #region FlyoutShowMode
    public static readonly StyledProperty<FlyoutShowMode> FlyoutShowModeProperty = AvaloniaProperty.Register<TextButtonFlyout, FlyoutShowMode>(
        "FlyoutShowMode", FlyoutShowMode.TransientWithDismissOnPointerMoveAway);

    public FlyoutShowMode FlyoutShowMode
    {
        get => GetValue(FlyoutShowModeProperty);
        set => SetValue(FlyoutShowModeProperty, value);
    }
    #endregion

    #region Header
    public static readonly StyledProperty<string> HeaderProperty = AvaloniaProperty.Register<TextButtonFlyout, string>(
        "Header");

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
    #endregion

    #region OnHoverColor
    public static readonly StyledProperty<IBrush?> OnHoverColorProperty = AvaloniaProperty.Register<TextButtonFlyout, IBrush?>(
        "OnHoverColor");

    public IBrush? OnHoverColor
    {
        get => GetValue(OnHoverColorProperty);
        set => SetValue(OnHoverColorProperty, value);
    }
    #endregion

    #region HorizontalOffset
    public static readonly StyledProperty<float> HorizontalOffsetProperty = AvaloniaProperty.Register<TextButtonFlyout, float>(
        "HorizontalOffset");

    public float HorizontalOffset
    {
        get => GetValue(HorizontalOffsetProperty);
        set => SetValue(HorizontalOffsetProperty, value);
    }
    #endregion
    
    #region Content
    public static readonly StyledProperty<object> ContentProperty = AvaloniaProperty.Register<TextButtonFlyout, object>(
        "Content");
    [Content]
    public object Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }
    #endregion
}