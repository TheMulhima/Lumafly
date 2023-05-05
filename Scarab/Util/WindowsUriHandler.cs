using System;
using System.IO;
using Microsoft.Win32;

namespace Scarab.Util;

public static class WindowsUriHandler
{
    
    const string UriScheme = "scarab";
    const string FriendlyName = "scarab protocol";

    public static void Setup()
    {
        using (var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + UriScheme))
        {
            string applicationLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scarab.exe");

            key.SetValue(null, "URL:" + FriendlyName);
            key.SetValue("URL Protocol",String.Empty, RegistryValueKind.String);
            
            using (var defaultIcon = key.CreateSubKey("DefaultIcon"))
            {
                string iconValue = String.Format("\"{0}\",0", applicationLocation);
                defaultIcon.SetValue(null, iconValue);
            }


            using (var commandKey = key.CreateSubKey(@"shell\open\command"))
            {
                string cmdValue = String.Format("\"{0}\" \"%1\"", applicationLocation);
                commandKey.SetValue(null, cmdValue);
            }
        }
    }
}