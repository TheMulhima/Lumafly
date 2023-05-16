using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Scarab.Util;

public enum UriCommands
{
    none,
    download,
    reset,
    forceUpdateAll,
    customModlinks,
    baseLink,
}

public static class WindowsUriHandler
{
    private const string UriScheme = "scarab";
    private const string FriendlyName = "scarab protocol";

    public static string Data = "";
    public static bool Handled = false;
    public static UriCommands UriCommand = UriCommands.none;

    private static readonly Dictionary<UriCommands, Action<string>?> AvailableCommands = new ()
    {
        {UriCommands.none, null},
        {UriCommands.download, s => Data = s},
        {UriCommands.reset, null},
        {UriCommands.forceUpdateAll, null},
        {UriCommands.customModlinks, s => Data = s},
        {UriCommands.baseLink, s => Data = s},
    };
    
    public static void SetCommand(string arg)
    {
        arg = arg.Trim();
        
        var UriPrefix = UriScheme + "://";
        var UriParam = 
            arg[UriPrefix.Length..]
            .Trim('/')
            .Replace("%20", " ");

        foreach(var (command, action) in AvailableCommands)
        {
            var commandName = command.ToString();
            if (!UriParam.StartsWith(commandName)) continue;
            
            UriCommand = command;
            action?.Invoke(UriParam[commandName.Length..].Trim('/'));
            break; // only 1 command allowed for now
        }
    }

    [SupportedOSPlatform(nameof(OSPlatform.Windows))]
    public static void SetupRegistry(string exePath)
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
    }
}