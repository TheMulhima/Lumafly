using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace Scarab.Util;

public class WindowsUriHandler
{
    const string UriScheme = "scarab";
    const string FriendlyName = "scarab protocol";

    public static string? ModDownload = null;
    
    public void SetDownload (string download)
    {
        string prefix = UriScheme + "://";
        ModDownload = download.Substring(prefix.Length).Trim('/').Replace("%20", " ");
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