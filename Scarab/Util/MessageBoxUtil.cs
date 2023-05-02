using System.Threading;

using Avalonia.Controls;
using Avalonia.Media;

using MessageBox.Avalonia;
using MessageBox.Avalonia.BaseWindows.Base;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;

namespace Scarab.Util;

internal static class MessageBoxUtil {
    internal static readonly string fontFamilyString;

    public static IMsBoxWindow<string> GetMessageBoxCustomWindow(MessageBoxCustomParams @params) {
        @params.FontFamily = fontFamilyString;
        return MessageBoxManager.GetMessageBoxCustomWindow(@params);
    }

    public static IMsBoxWindow<string> GetMessageBoxCustomWindow(MessageBoxCustomParamsWithImage @params) {
        @params.FontFamily = fontFamilyString;
        return MessageBoxManager.GetMessageBoxCustomWindow(@params);
    }

    public static IMsBoxWindow<ButtonResult> GetMessageBoxStandardWindow(MessageBoxStandardParams @params) {
        @params.FontFamily = fontFamilyString;
        return MessageBoxManager.GetMessageBoxStandardWindow(@params);
    }

    public static IMsBoxWindow<ButtonResult> GetMessageBoxHyperlinkWindow(MessageBoxHyperlinkParams @params) {
        @params.FontFamily = fontFamilyString;
        return MessageBoxManager.GetMessageBoxHyperlinkWindow(@params);
    }

    public static IMsBoxWindow<ButtonResult> GetMessageBoxStandardWindow(string title, string text, ButtonEnum @enum = ButtonEnum.Ok, Icon icon = Icon.None, WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterScreen) =>
        GetMessageBoxStandardWindow(new MessageBoxStandardParams {
            ContentTitle = title,
            ContentMessage = text,
            ButtonDefinitions = @enum,
            Icon = icon,
            WindowStartupLocation = windowStartupLocation
        });

    public static IMsBoxWindow<MessageWindowResultDTO> GetMessageBoxInputWindow(MessageBoxInputParams @params) {
        @params.FontFamily = fontFamilyString;
        return MessageBoxManager.GetMessageBoxInputWindow(@params);
    }

    static MessageBoxUtil() =>
        fontFamilyString = Program.fontOverrides.TryGetValue(
            Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName,
            out string? fontFamily
        ) ? fontFamily : FontFamily.Default.FamilyNames.ToString();
}
