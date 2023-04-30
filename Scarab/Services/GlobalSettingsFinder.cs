using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Scarab.Interfaces;
using Scarab.Models;

namespace Scarab.Services;

public class GlobalSettingsFinder
{
    private readonly ISettings Settings;

    private static GlobalSettingsFinder? _instance;
    public static GlobalSettingsFinder Instance => _instance ?? throw new Exception("GlobalSettingsFinder not initialized");

    public GlobalSettingsFinder(ISettings settings)
    {
        Settings = settings;
        _instance = this;
    }

    public static string GetSavesFolder()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var savesFolder = string.Empty;

        if (OperatingSystem.IsWindows())
            savesFolder = Path.Combine(userProfile, "AppData", "LocalLow", "Team Cherry", "Hollow Knight");
        if (OperatingSystem.IsMacOS())
            savesFolder = Path.Combine(userProfile, "Library", "Application Support", "unity.Team Cherry.Hollow Knight");
        if (OperatingSystem.IsLinux())
            savesFolder = Path.Combine(userProfile, ".config", "unity3d", "Team Cherry", "Hollow Knight");

        return savesFolder;
    }

    private readonly string[] ModBaseClasses = new[]
    {
        "Modding.Mod", // main one
        "SFCore.Generics.SaveSettingsMod",
        "SFCore.Generics.FullSettingsMod",
        "SFCore.Generics.GlobalSettingsMod",
        "Satchel.BetterPreloads.BetterPreloadsMod",
    };
    
    public string? GetSettingsFile(ModItem modItem, string? savesFolder = null, string? modsFolder = null)
    {
        try
        {
            savesFolder ??= GetSavesFolder();
            
            var exactPath = GetGSFileName(savesFolder, modItem.Name);
            
            if (File.Exists(exactPath)) 
                return exactPath;
            
            var strippedPath = GetGSFileName(savesFolder, modItem.Name.Replace(" ", string.Empty));
            
            if (File.Exists(strippedPath)) 
                return strippedPath;
            
            var pathWithModSuffix = GetGSFileName(savesFolder, modItem.Name + "Mod");
            
            if (File.Exists(pathWithModSuffix)) 
                return pathWithModSuffix;
            
            var result = TryGettingModClassName(modItem, savesFolder, modsFolder);
            
            return result != null ? GetGSFileName(savesFolder, result) : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private string? TryGettingModClassName(ModItem modItem, string savesFolder, string? modsFolder)
    {
        if (modItem.State is not InstalledState state)
            return null;

        modsFolder ??= state.Enabled ? Settings.ModsFolder : Settings.DisabledFolder;
        
        string modItemFolder = Path.Combine(modsFolder, modItem.Name);

        if (!Directory.Exists(modItemFolder))
            return null;

        foreach (var dll in Directory.GetFiles(modItemFolder).Where(x => x.EndsWith(".dll")))
        {
            using var asmDefinition = AssemblyDefinition.ReadAssembly(dll);
            foreach (var ty in asmDefinition.MainModule.Types.Where(ty => ty.IsClass && !ty.IsAbstract))
            {
                if (ModBaseClasses.Any(x => ty.BaseType is not null && ty.BaseType.FullName.StartsWith(x)) &&
                    File.Exists(GetGSFileName(savesFolder, ty.Name)))
                {
                        return ty.Name;
                }
            }
        }
        return null;
    }

    private string GetGSFileName(string savesFolder, string modName) =>
        Path.Combine(savesFolder, modName + ".GlobalSettings.json");
}