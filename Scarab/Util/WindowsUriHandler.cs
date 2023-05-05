using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace Scarab.Util;

public enum Commands
{
    none,
    download
}

public class WindowsUriHandler
{
    const string UriScheme = "scarab";
    const string FriendlyName = "scarab protocol";

    public static string Mod = "";
    public static Commands Command = Commands.none;
    
    public void SetDownload (string download)
    {
        string prefix = UriScheme + "://";
        var command = download[prefix.Length..].Trim('/').Replace("%20", " ");
        
        string downloadPrefix = $"{Commands.download.ToString()}/";
        
        if (command.StartsWith(downloadPrefix))
        {
            Command = Commands.download;
            Mod = command[downloadPrefix.Length..].Trim();
        }
    }

    public void SetupRegistry(string exePath)
    {
        try
        {
            using (var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + UriScheme))
            {
                key.SetValue(null, "URL:" + FriendlyName);
                key.SetValue("URL Protocol", String.Empty, RegistryValueKind.String);

                using (var defaultIcon = key.CreateSubKey("DefaultIcon"))
                {
                    string iconValue = String.Format("\"{0}\",0", exePath);
                    defaultIcon.SetValue(null, iconValue);
                }


                using (var commandKey = key.CreateSubKey(@"Shell\Open\Command"))
                {
                    string cmdValue = String.Format("\"{0}\" \"%1\"", exePath);
                    commandKey.SetValue(null, cmdValue);
                }
            }
        }
        catch (Exception e)
        {
            // for now not show any error as its not critical
            Trace.WriteLine("Unable to setup registry for windows uri scheme" + e.Message);
        }
    }
}