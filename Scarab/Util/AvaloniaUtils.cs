using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Scarab.Enums;
using Scarab.Views.Windows;

namespace Scarab.Util;

/// <summary>
/// For having the UI how we want we got to do some reflection cuz API doesn't expose it :)
/// </summary>
public static class AvaloniaUtils
{
    /// <summary>
    /// it needs to be done like this because :pointerover doesnt accept bindings and
    /// so the only option is to change the :pointerover setters directly
    /// </summary>
    public static void SetStyleSetterByName(this ContentControl content, string styleSelectorName, AvaloniaProperty propertyToSet, object? newSetterValue)
    {
        if (content.Styles.FirstOrDefault(x => (x as Style)?.ToString().Contains(styleSelectorName) ?? false) is not Style style)
            throw new Exception("Could not find pointer over style");
    
        int index = style.Setters.IndexOf(style.Setters.First(x => x is Setter setter && setter.Property == propertyToSet));
        style.Setters[index] = new Setter(propertyToSet, newSetterValue!);
    }
    
    public static readonly IValueConverter HowRecentToBoolConvertor =
        new FuncValueConverter<HowRecentModChanged, bool>((howRecent) => howRecent == 0);

    
    public static T? GetFirstChild<T>(this Control? control) where T : Control
    {
        if (control == null) return null;
        if (!control.GetLogicalChildren().Any()) return null;
        
        return control.GetLogicalChildren().OfType<T>().First();
    }

    static AvaloniaUtils()
    {
        MenuItemPopup = null!;
        DoReflections();
    }

    private static FieldInfo MenuItemPopup;

    public static void DoReflections()
    {
        var _menuItemPopup = typeof(MenuItem).GetField("_popup", BindingFlags.Instance | BindingFlags.NonPublic);
        MenuItemPopup = _menuItemPopup ?? throw new Exception("MenuItem Popup field not found");
    }

    public static Popup GetPopup(this MenuItem menuItem)
    {
        var popup_object = MenuItemPopup.GetValue(menuItem);
        return popup_object as Popup ?? throw new Exception("MenuItem popup not found");
    }
  
  
    public static MainWindow GetMainWindow() => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow as MainWindow
                                            ?? throw new InvalidOperationException();


    /// <summary>
    /// Checks if the current process is running as administrator.
    /// </summary>
    [SupportedOSPlatform(nameof(OSPlatform.Windows))]
    public static bool IsAdministrator()
    {
        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}

/// <summary>
/// A value convertor to be used in the AXAML to convert between <see cref="HowRecentModChanged"/> so that a var of type
/// <see cref="HowRecentModChanged"/> can be in ViewModel.
/// </summary>
public class HowRecentEnumToBoolConvertor : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null || targetType != typeof(bool?)) return AvaloniaProperty.UnsetValue;

        return (HowRecentModChanged) value == (HowRecentModChanged) parameter;
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null || targetType != typeof(HowRecentModChanged)) return AvaloniaProperty.UnsetValue;

        return (HowRecentModChanged)parameter switch
        {
            HowRecentModChanged.Month => (bool)value ? HowRecentModChanged.Month : HowRecentModChanged.Week,
            HowRecentModChanged.Week => (bool)value ? HowRecentModChanged.Week : HowRecentModChanged.Month,
            _ => AvaloniaProperty.UnsetValue
        };
    }
}