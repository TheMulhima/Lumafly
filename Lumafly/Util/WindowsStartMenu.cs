using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Lumafly.Util;

public static class WindowsStartMenu
{
    public static void CreateShortcutInStartMenu(string appName, string appPath)
    {
        try
        {
            // Define the path to the Start Menu Programs folder
            string startMenuFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");

            // Create a shortcut file path
            string shortcutPath = Path.Combine(startMenuFolderPath, $"{appName}.lnk");

            // Create a shortcut object
            IShellLink shellLink = (IShellLink)new ShellLink();

            // Set the properties of the shortcut
            shellLink.SetPath(appPath); // Path to your application's .exe
            shellLink.SetDescription($"Shortcut to {appName}"); // Description
            shellLink.SetIconLocation(appPath, 0); // Path to the icon for the shortcut (can be your application's .exe)

            // Save the shortcut
            ((IPersistFile)shellLink).Save(shortcutPath, false);

            Console.WriteLine($"Shortcut to {appName} created in the Start Menu.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating shortcut: {ex.Message}");
        }
    }

    // Define COM interfaces for IShellLink and IPersistFile
    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLink { }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal interface IShellLink
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotKey(out short wHotKey);
        void SetHotKey(short wHotKey);
        void GetShowCmd(out uint iShowCmd);
        void SetShowCmd(uint iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [Guid("0000010B-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPersistFile
    {
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
        [PreserveSig]
        int IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
    }
}
