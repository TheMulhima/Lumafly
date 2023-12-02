using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Win32;
using MsBox.Avalonia.Dto;
using Avalonia.Threading;
using MsBox.Avalonia.Enums;
using Lumafly.Enums;
using Lumafly.Interfaces;

namespace Lumafly.Util;

public class UrlSchemeHandler : IUrlSchemeHandler
{
    private const string UriScheme = "scarab";
    private const string FriendlyName = "scarab protocol";

    public string Data { get; private set; } = "";
    public bool Handled {get; protected set;} = false;
    public UrlSchemeCommands UrlSchemeCommand {get; private set;} = UrlSchemeCommands.none;

    private readonly Dictionary<UrlSchemeCommands, Action<string>?> AvailableCommands;

    public UrlSchemeHandler(bool handled = false)
    {
        Handled = handled;
        AvailableCommands = new Dictionary<UrlSchemeCommands, Action<string>?>
        {
            {UrlSchemeCommands.none, null},
            {UrlSchemeCommands.download, s => Data = s},
            {UrlSchemeCommands.reset, null},
            {UrlSchemeCommands.forceUpdateAll, null},
            {UrlSchemeCommands.customModLinks, s => Data = s},
            {UrlSchemeCommands.removeAllModsGlobalSettings, null},
            {UrlSchemeCommands.removeGlobalSettings, s => Data = s},
            {UrlSchemeCommands.useOfficialModLinks, null},
            {UrlSchemeCommands.launch, s => Data = s},
            {UrlSchemeCommands.modpack, s => Data = s},
        };
    }

    public void SetCommand(UrlSchemeCommands arg)
    {
        UrlSchemeCommand = arg;
    }
    
    public void SetCommand(string arg)
    {
        if (Handled) return;

        arg = arg.Trim();
        
        var UriPrefix = UriScheme + "://";

        if (arg.Length < UriPrefix.Length || !arg.StartsWith(UriPrefix))
        {
            Dispatcher.UIThread.InvokeAsync(async () =>
                await ShowConfirmation(
                    title: "Invalid URL Scheme Command", 
                    message: $"{arg} is an invalid command.\\nLumafly only accepts command prefixed by scarab://", 
                    Icon.Warning));
            return;
        }

        var UriParam = arg[UriPrefix.Length..].Trim('/');
        UriParam = HttpUtility.UrlDecode(UriParam);

        foreach(var (command, setData) in AvailableCommands)
        {
            var commandName = command.ToString();
            if (!UriParam.StartsWith(commandName)) continue;
            
            UrlSchemeCommand = command;
            setData?.Invoke(UriParam[commandName.Length..].Trim('/'));
            break; // only 1 command allowed for now
        }

        if (UrlSchemeCommand == UrlSchemeCommands.none && !string.IsNullOrEmpty(UriParam))
        {
            Dispatcher.UIThread.InvokeAsync(async () =>
                await ShowConfirmation(
                    title: "Invalid URL Scheme Command",
                    message: $"{arg}  is an invalid command.\\nIt was not found in lumafly's accepted command list",
                    Icon.Warning));
        }
    }

    public Dictionary<string, string?> ParseDownloadCommand(string data)
    {
        int index = 0;
        Dictionary<string, string?> modNamesAndUrls = new Dictionary<string, string?>();
        
        while (index < data.Length)
        {
            string modName = string.Empty;
            string? url = null;
            while (index < data.Length && data[index] != '/')
            {
                if (data[index] == ':') // starter of url
                {
                    index++; // consume :
                    
                    const char LinkSep = '\'';
                    
                    if (index >= data.Length || data[index] != LinkSep) return new Dictionary<string, string?>(); // invalid format refuse to parse
                    
                    index++; // consume "
                    while (index < data.Length && data[index] != LinkSep)
                    {
                        url += data[index];
                        index++;
                    }

                    if (index < data.Length && data[index] == LinkSep)
                        index++; // consume "
                    break;
                }

                modName += data[index];
                index++;
            }

            try { _ = new Uri(url ?? throw new Exception()); }
            catch { url = null; }

            // windows folder regex
            var invalidChars = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars());
            if (invalidChars.Any(x => modName.Contains(x))) return new Dictionary<string, string?>(); // invalid string refuse to parse
            
            modNamesAndUrls[modName] = url;
            index++; // consume /
        }

        return modNamesAndUrls;
    }

    public static void Setup()
    {
        if (OperatingSystem.IsWindows())
        {
            SetupWindows(Environment.GetCommandLineArgs()[0]);
        }
        if (OperatingSystem.IsLinux())
        {
            SetupLinux(Environment.GetCommandLineArgs()[0]);
        }
        
        // the mac way of doing it is the only sane one where we add what it needs to the Info.plist at compile time
    }

    [SupportedOSPlatform(nameof(OSPlatform.Windows))]
    private static void SetupWindows(string exePath)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + UriScheme);
            
            key.SetValue(null, "URL:" + FriendlyName);
            key.SetValue("URL Protocol", string.Empty, RegistryValueKind.String);

            using (var defaultIcon = key.CreateSubKey("DefaultIcon"))
            {
                var iconValue = $"\"{exePath}\",0";
                defaultIcon.SetValue(null, iconValue);
            }

            using (var commandKey = key.CreateSubKey(@"Shell\Open\Command"))
            {
                var cmdValue = $"\"{exePath}\" \"%1\"";
                commandKey.SetValue(null, cmdValue);
            }
        }
        catch (Exception e)
        {
            // for now not show any error as its not critical
            Trace.WriteLine("Unable to setup registry for windows uri scheme" + e.Message);
        }
        
        try
        {
            WindowsStartMenu.CreateShortcutInStartMenu("Lumafly", exePath);
        }
        catch (Exception e)
        {
            // for now not show any error as its not critical
            Trace.WriteLine("Unable to setup start menu shortcut for windows" + e.Message);
        }
    }
    
    [SupportedOSPlatform(nameof(OSPlatform.Linux))]
    private static void SetupLinux(string exePath)
    {
        try
        {
            // /usr/share/applications/scarab.desktop
            var _desktopLocations = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData, Environment.SpecialFolderOption.Create),
                "applications");
            
            FileUtil.CreateDirectory(_desktopLocations);

            var desktopFile = Path.Combine(_desktopLocations, "scarab.desktop");
            
            File.WriteAllText(desktopFile,
            $""" 
            [Desktop Entry]
            Name=Lumafly
            Comment=Hollow Knight Mod Manager
            GenericName=Lumafly
            Exec={exePath} %U
            Type=Application
            StartupNotify=true
            Categories=GNOME;GTK;Utility;
            MimeType=x-scheme-handler/scarab
            """);
        }
        catch (Exception e)
        {
            // for now not show any error as its not critical
            Trace.WriteLine("Unable to setup for linux url scheme" + e.Message);
        }
    }

    public async Task ShowConfirmation(MessageBoxStandardParams param)
    {
        if (Handled) return;
        
        Handled = true;
        await MessageBoxUtil.GetMessageBoxStandardWindow(param).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());
    }
    
    /// <summary>
    /// To be called when finished handling the url scheme when you want to show a confirmation box
    /// </summary>
    public async Task ShowConfirmation(string title, string message, Icon icon = Icon.Success)
    {
        await ShowConfirmation(new MessageBoxStandardParams()
        {
            ContentTitle = title,
            ContentMessage = message,
            Icon = icon,
            MinWidth = 450,
            MinHeight = 150,
        });
    }
    
    /// <summary>
    /// To be called when finished handling the url scheme when you don't want to show a confirmation box
    /// </summary>
    public void FinishHandlingUrlScheme()
    {
        Handled = true;
    }
}