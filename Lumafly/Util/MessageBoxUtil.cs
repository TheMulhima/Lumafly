using System.Threading;
using Avalonia.Controls;
using Avalonia.Media;
using Lumafly.Views.Windows;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Base;

namespace Lumafly.Util;

internal static class MessageBoxUtil {
    internal static readonly string fontFamilyString;

    public static IMsBox<string> GetMessageBoxCustomWindow(MessageBoxCustomParams @params) {
        @params.FontFamily = fontFamilyString;
        return MessageBoxManager.GetMessageBoxCustom(@params);
    }

    public static IMsBox<ButtonResult> GetMessageBoxStandardWindow(MessageBoxStandardParams @params) {
        @params.FontFamily = fontFamilyString;
        return MessageBoxManager.GetMessageBoxStandard(@params);
    }

    public static IMsBox<ButtonResult> GetMessageBoxStandardWindow(string title, string text, ButtonEnum @enum = ButtonEnum.Ok, Icon icon = Icon.None, WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterScreen) =>
        GetMessageBoxStandardWindow(new MessageBoxStandardParams {
            ContentTitle = title,
            ContentMessage = text,
            ButtonDefinitions = @enum,
            Icon = icon,
            WindowStartupLocation = windowStartupLocation,
            MaxWidth = AvaloniaUtils.GetMainWindow().Width,
        });

    static MessageBoxUtil() =>
        fontFamilyString = Program.fontOverrides.TryGetValue(
            Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName,
            out string? fontFamily
        ) ? fontFamily : FontFamily.Default.FamilyNames.ToString();
}
