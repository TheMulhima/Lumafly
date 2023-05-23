using System;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Styling;

namespace Scarab.Extensions;

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
    
    public static Window GetMainWindow() => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
                                            ?? throw new InvalidOperationException();
}