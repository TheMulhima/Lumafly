using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using ReactiveUI;
using Scarab.Util;

namespace Scarab.Views.Controls;

/// <summary>
/// A templated control that represents a menu checkbox with exclusion feature.
/// Please set values for the Header and IsSelected properties.
/// </summary>
public class ExcludableCheckBox : TemplatedControl
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

        InternalOnExcludePress = ReactiveCommand.Create(() =>
        {
            IsExcluded = !IsExcluded;
            OnExclude?.Execute(null);
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
    
    public static readonly DirectProperty<ExcludableCheckBox, bool> IsSelectedProperty = AvaloniaProperty.RegisterDirect<ExcludableCheckBox, bool>(
        "IsSelected", o => o.IsSelected, (o, v) => o.IsSelected = v, defaultBindingMode: BindingMode.TwoWay);

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            SetAndRaise(IsSelectedProperty, ref _isSelected, value);
            if (value) IsExcluded = false;
            if (_initialized)
                SetButtonColors();
        }
    }
    #endregion

    #region IsExcluded
    private bool _isExcluded;

    public static readonly DirectProperty<ExcludableCheckBox, bool> IsExcludedProperty = AvaloniaProperty.RegisterDirect<ExcludableCheckBox, bool>(
        "IsExcluded", o => o.IsExcluded, (o, v) => o.IsExcluded = v, defaultBindingMode: BindingMode.TwoWay);

    public bool IsExcluded
    {
        get => _isExcluded;
        set
        {
            SetAndRaise(IsExcludedProperty, ref _isExcluded, value);
            if (value) IsSelected = false;
            if (_initialized)
                SetButtonColors();
        }
    }

    #endregion

    #region InternalOnPress
    private ICommand? _internalOnPress;

    public static readonly DirectProperty<ExcludableCheckBox, ICommand?> InternalOnPressProperty = AvaloniaProperty.RegisterDirect<ExcludableCheckBox, ICommand?>(
        "OnPress", o => o.InternalOnPress, (o, v) => o.InternalOnPress = v);

    private ICommand? InternalOnPress
    {
        get => _internalOnPress;
        set => SetAndRaise(InternalOnPressProperty, ref _internalOnPress, value);
    }
    #endregion

    #region InternalOnExcludePress
    private ICommand? _internalOnExcludePress;

    public static readonly DirectProperty<ExcludableCheckBox, ICommand?> InternalOnExcludePressProperty = AvaloniaProperty.RegisterDirect<ExcludableCheckBox, ICommand?>(
        "OnExcludePress", o => o.InternalOnExcludePress, (o, v) => o.InternalOnExcludePress = v);

    private ICommand? InternalOnExcludePress
    {
        get => _internalOnExcludePress;
        set => SetAndRaise(InternalOnExcludePressProperty, ref _internalOnExcludePress, value);
    }
    #endregion

    #region OnSelect
    public static readonly StyledProperty<ICommand?> OnSelectProperty = AvaloniaProperty.Register<ExcludableCheckBox, ICommand?>(
        "OnSelect");

    public ICommand? OnSelect
    {
        get => GetValue(OnSelectProperty);
        set => SetValue(OnSelectProperty, value);
    }
    #endregion

    #region OnExclude
    public static readonly StyledProperty<ICommand?> OnExcludeProperty = AvaloniaProperty.Register<ExcludableCheckBox, ICommand?>(
        "OnExclude");

    public ICommand? OnExclude
    {
        get => GetValue(OnExcludeProperty);
        set => SetValue(OnExcludeProperty, value);
    }
    #endregion

    #region Header
    public static readonly StyledProperty<string> HeaderProperty = AvaloniaProperty.Register<ExcludableCheckBox, string>(
        "Header", "Select Me");

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
    #endregion
    
    #region SelectedColor
    public static readonly StyledProperty<IBrush?> SelectedColorProperty = AvaloniaProperty.Register<ExcludableCheckBox, IBrush?>(
        "SelectedColor", Application.Current!.Resources["HighlightBlue"] as IBrush);

    public IBrush? SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }
    #endregion
    
    #region HoverColor
    public static readonly StyledProperty<IBrush?> HoverColorProperty = AvaloniaProperty.Register<ExcludableCheckBox, IBrush?>(
        "HoverColor", Application.Current.Resources["DefaultButtonColor"] as IBrush);

    public IBrush? HoverColor
    {
        get => GetValue(HoverColorProperty);
        set => SetValue(HoverColorProperty, value);
    }
    #endregion
}