using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using ReactiveUI;
using Scarab.Extensions;

namespace Scarab.Views.Controls;

/// <summary>
/// A templated control that represents a menu checkbox.
/// Please set values for the Header and IsSelected properties.
/// </summary>
public class MenuCheckBox : TemplatedControl
{
    private Button? SelectableButton;

    private bool _initialized = false;
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        SelectableButton = e.NameScope.Find<Button>(nameof(SelectableButton));
        
        InternalOnPress = ReactiveCommand.Create(() =>
        {
            IsSelected = !IsSelected;
            OnSelect?.Execute(null);
            SetButtonColors();
        });
        
        SetButtonColors();
        _initialized = true;
    }

    private void SetButtonColors()
    {
        SelectableButton = SelectableButton ?? throw new Exception("Menu Checkbox doesnt have button");
        SelectableButton.Background = IsSelected ? SelectedColor : Brushes.Transparent;
        
        // it needs to be done like this because :pointerover doesnt accept bindings and
        // so the only option is to change the :pointerover setters directly
        SelectableButton.SetStyleSetterByName("Button:pointerover", BackgroundProperty, IsSelected ? SelectedColor : HoverColor);
    }

    #region IsSelected
    private bool _isSelected;
    
    public static readonly DirectProperty<MenuCheckBox, bool> IsSelectedProperty = AvaloniaProperty.RegisterDirect<MenuCheckBox, bool>(
        "IsSelected", o => o.IsSelected, (o, v) => o.IsSelected = v, defaultBindingMode: BindingMode.TwoWay);

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            SetAndRaise(IsSelectedProperty, ref _isSelected, value);
            if (_initialized)
                SetButtonColors();
        }
    }
    #endregion
    
    #region InternalOnPress
    private ICommand? _internalOnPress;

    public static readonly DirectProperty<MenuCheckBox, ICommand?> InternalOnPressProperty = AvaloniaProperty.RegisterDirect<MenuCheckBox, ICommand?>(
        "OnPress", o => o.InternalOnPress, (o, v) => o.InternalOnPress = v);

    private ICommand? InternalOnPress
    {
        get => _internalOnPress;
        set => SetAndRaise(InternalOnPressProperty, ref _internalOnPress, value);
    }
    #endregion

    #region OnSelect
    public static readonly StyledProperty<ICommand?> OnSelectProperty = AvaloniaProperty.Register<MenuCheckBox, ICommand?>(
        "OnSelect");

    public ICommand? OnSelect
    {
        get => GetValue(OnSelectProperty);
        set => SetValue(OnSelectProperty, value);
    }
    #endregion

    #region Header
    public static readonly StyledProperty<string> HeaderProperty = AvaloniaProperty.Register<MenuCheckBox, string>(
        "Header", "Select Me");

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
    #endregion
    
    #region SelectedColor
    public static readonly StyledProperty<IBrush?> SelectedColorProperty = AvaloniaProperty.Register<MenuCheckBox, IBrush?>(
        "SelectedColor", Application.Current!.Resources["HighlightBlue"] as IBrush);

    public IBrush? SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }
    #endregion
    
    #region HoverColor
    public static readonly StyledProperty<IBrush?> HoverColorProperty = AvaloniaProperty.Register<MenuCheckBox, IBrush?>(
        "HoverColor", Application.Current.Resources["DefaultButtonColor"] as IBrush);

    public IBrush? HoverColor
    {
        get => GetValue(HoverColorProperty);
        set => SetValue(HoverColorProperty, value);
    }
    #endregion
}