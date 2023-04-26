using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Metadata;
using FluentIcons.Avalonia;
using FluentIcons.Common;
using Scarab.Extensions;

namespace Scarab.CustomControls;

/// <summary>
/// Creates a button with an attached flyout.
/// Only supports FlyoutPlacementMode.Bottom and FlyoutPlacementMode.Right.
/// </summary>
public class TextButtonFlyout : TemplatedControl
{
    private readonly Stopwatch lastOpenedStopwatch = new();
    private SymbolIcon? Icon;
    private Button? Button;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Icon = e.NameScope.Find<SymbolIcon>("Icon");
        Button = e.NameScope.Find<Button>("Button");

        Button.Flyout = new Flyout()
        {
            Placement = FlyoutPlacement,
            ShowMode = FlyoutShowMode,
            Content = Content
        };
        var popup_object = typeof(FlyoutBase).GetProperty(nameof(Popup), BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Button.Flyout);
        var popup = popup_object as Popup ?? throw new Exception("FlyoutButton popup not found");
        
        popup.HorizontalOffset = HorizontalOffset;
        
        Button.Flyout.Opened += OnFlyoutOpened;
        Button.Flyout.Closing += OnFlyoutClosing;
        Button.Flyout.Closed += OnFlyoutClosed;

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
        Icon.Symbol = Button.Flyout.IsOpen ? GetCloseSymbol() : GetOpenSymbol();
    }

    private Symbol GetOpenSymbol() => FlyoutPlacement switch
    {
        FlyoutPlacementMode.Bottom or
            FlyoutPlacementMode.BottomEdgeAlignedLeft or
            FlyoutPlacementMode.BottomEdgeAlignedRight => Symbol.ChevronDown,

        FlyoutPlacementMode.Right or
            FlyoutPlacementMode.RightEdgeAlignedBottom or
            FlyoutPlacementMode.RightEdgeAlignedTop => Symbol.ChevronRight,

        _ => throw new NotImplementedException()
    };

    private Symbol GetCloseSymbol() => FlyoutPlacement switch
    {
        FlyoutPlacementMode.Bottom or
            FlyoutPlacementMode.BottomEdgeAlignedLeft or
            FlyoutPlacementMode.BottomEdgeAlignedRight => Symbol.ChevronUp,

        FlyoutPlacementMode.Right or
            FlyoutPlacementMode.RightEdgeAlignedBottom or
            FlyoutPlacementMode.RightEdgeAlignedTop => Symbol.ChevronLeft,

        _ => throw new NotImplementedException()
    };


    #region FlyoutPlacement
    private FlyoutPlacementMode _flyoutPlacement;

    public static readonly DirectProperty<TextButtonFlyout, FlyoutPlacementMode> FlyoutPlacementProperty = AvaloniaProperty.RegisterDirect<TextButtonFlyout, FlyoutPlacementMode>(
        "FlyoutPlacement", o => o.FlyoutPlacement, (o, v) => o.FlyoutPlacement = v, FlyoutPlacementMode.Bottom);

    public FlyoutPlacementMode FlyoutPlacement
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