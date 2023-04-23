using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Scarab.Extensions;

public static class SetStyleSetter
{
    // it needs to be done like this because :pointerover doesnt accept bindings and
    // so the only option is to change the :pointerover setters directly
    public static void SetStyleSetterByName(this ContentControl content, string styleSelectorName, AvaloniaProperty propertyToSet, object? newSetterValue)
    {
        if (content.Styles.FirstOrDefault(x => (x as Style)?.ToString().Contains(styleSelectorName) ?? false) is not Style style)
            throw new Exception("Could not find pointer over style");
    
        int index = style.Setters.IndexOf(style.Setters.First(x => x is Setter setter && setter.Property == propertyToSet));
        style.Setters[index] = new Setter(propertyToSet, newSetterValue!);

    }
}