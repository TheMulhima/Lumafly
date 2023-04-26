using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Scarab.Interfaces;
using Scarab.Models;

namespace Scarab.Services;

public static class GlobalSettingsFinder
{

    // set by MainWindowViewModel
    public static ISettings? Settings = null;
    
    public static string GetSavesFolder()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var savesFolder = string.Empty;

        if (OperatingSystem.IsWindows())
            savesFolder = Path.Combine(userProfile, "AppData", "LocalLow", "Team Cherry", "Hollow Knight");
        if (OperatingSystem.IsMacOS())
            savesFolder = Path.Combine(userProfile, "Library", "Application Support", "unity.Team Cherry.Hollow Knight");
        if (OperatingSystem.IsMacOS())
            savesFolder = Path.Combine(userProfile, ".config", "unity3d", "Team Cherry", "Hollow Knight");

        return savesFolder;
    }

    public static string[] ModBaseClasses = new[]
    {
        "Modding.Mod", // main one
        "SFCore.Generics.SaveSettingsMod",
        "SFCore.Generics.FullSettingsMod",
        "SFCore.Generics.GlobalSettingsMod",
        "Satchel.BetterPreloads.BetterPreloadsMod",
    };
    
    public static string? HasSettingsFile(ModItem modItem)
    {
        try
        {
            var savesFolder = GetSavesFolder();
            
            var exactPath = GetGSFileName(savesFolder, modItem.Name);
            
            if (File.Exists(exactPath)) 
                return exactPath;
            
            var strippedPath = GetGSFileName(savesFolder, modItem.Name.Trim());
            
            if (File.Exists(strippedPath)) 
                return strippedPath;
            
            var pathWithModSuffix = GetGSFileName(savesFolder, modItem.Name + "Mod");
            
            if (File.Exists(pathWithModSuffix)) 
                return pathWithModSuffix;
            
            var result = TryGettingModClassName(modItem, savesFolder);
            
            return result != null ? GetGSFileName(savesFolder, result) : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? TryGettingModClassName(ModItem modItem, string savesFolder)
    {
        if (Settings == null)
            return null;

        if (modItem.State is not InstalledState state)
            return null;
            
        string modItemFolder = Path.Combine(
            state.Enabled ? Settings.ModsFolder : Settings.DisabledFolder, 
            modItem.Name);
            
        if (!Directory.Exists(Settings.ModsFolder) || 
            !Directory.Exists(modItemFolder))
            return null;

        foreach (var dll in Directory.GetFiles(modItemFolder).Where(x => x.EndsWith(".dll")))
        {
            using var asmDefinition = AssemblyDefinition.ReadAssembly(dll);
            foreach (var ty in asmDefinition.MainModule.Types.Where(ty => ty.IsClass && !ty.IsAbstract))
            {
                if (ModBaseClasses.Any(x => ty.BaseType is not null && ty.BaseType.FullName.StartsWith(x))
                    && File.Exists(GetGSFileName(savesFolder, ty.Name)))
                    return ty.Name;
            }
        }
        return null;
    }

    private static string GetGSFileName(string savesFolder, string modName) =>
        Path.Combine(savesFolder, modName + ".GlobalSettings.json");
}