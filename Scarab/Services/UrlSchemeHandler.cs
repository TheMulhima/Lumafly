using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.Win32;
using MessageBox.Avalonia.DTO;
using Avalonia.Threading;
using MessageBox.Avalonia.Enums;
using Scarab.Interfaces;

namespace Scarab.Util;

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
            {UrlSchemeCommands.baseLink, s => Data = s},
        };
    }
    
    public void SetCommand(string arg)
    {
        if (Handled) return;
        
        arg = arg.Trim();
        
        var UriPrefix = UriScheme + "://";

        if (arg.Length < UriPrefix.Length || !arg.StartsWith(UriPrefix))
        {
            Task.Run(async () => await Dispatcher.UIThread.InvokeAsync(async () =>
                await ShowConfirmation(new MessageBoxStandardParams()
                {
                    ContentTitle = "Invalid URL Scheme Command",
                    ContentMessage = $"{arg} is an invalid command.\nScarab only accepts command prefixed by scarab://",
                    MinWidth = 450,
                    MinHeight = 150,
                    Icon = Icon.Warning,
                })));
            return;
        }
        
        var UriParam = 
            arg[UriPrefix.Length..]
            .Trim('/')
            .Replace("%20", " ");

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
            Task.Run(async () => await Dispatcher.UIThread.InvokeAsync(async () =>
                await ShowConfirmation(new MessageBoxStandardParams()
                {
                    ContentTitle = "Invalid URL Scheme Command",
                    ContentMessage = $"{arg} is an invalid command.\nIt was not found in scarab's accepted command list",
                    MinWidth = 450,
                    MinHeight = 150,
                    Icon = Icon.Warning,
                })));
        }
    }

    [SupportedOSPlatform(nameof(OSPlatform.Windows))]
    public static void SetupWindows(string exePath)
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

    [SupportedOSPlatform(nameof(OSPlatform.Linux))]
    public static void SetupLinux(string path)
    {
        try
        {
            var desktopFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), 
                "scarab.desktop");
            var desktopFileContent = $"[Desktop Entry]\nName=Scarab\nExec={path} %u\nType=Application\nTerminal=false\nMimeType=x-scheme-handler/scarab;";
            File.WriteAllText(desktopFile, desktopFileContent);
            
            var mimeFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), 
                "mimeapps.list");
            
            //var mimeFileContent = $"[Default Applications]\nx-scheme-handler/scarab=scarab.desktop";
            string mimeFileContent = string.Empty;
            if (File.Exists(mimeFile))
            {
                mimeFileContent = File.ReadAllText(mimeFile);
            } 
            if (mimeFileContent.Contains("x-scheme-handler/scarab=scarab.desktop")) return;
            if (mimeFileContent.Contains("[Added Associations]"))
            {
                mimeFileContent = mimeFileContent.Replace("[Added Associations]",
                    "[Added Associations]\nx-scheme-handler/scarab=scarab.desktop;");
            }
            else
            {
                mimeFileContent += "\n[Default Applications]\nx-scheme-handler/scarab=scarab.desktop;";
            }
            File.WriteAllText(mimeFile, mimeFileContent);
        }
        catch (Exception e)
        {
            Trace.TraceError("Unable to setup linux url scheme" + e.Message);
        }
    }

    public async Task ShowConfirmation(MessageBoxStandardParams param)
    {
        if (Handled) return;
        
        Handled = true;
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
             await MessageBoxUtil.GetMessageBoxStandardWindow(param).Show();
        });
    }
}