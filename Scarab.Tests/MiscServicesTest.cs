using System;
using System.IO;
using Avalonia.Controls;
using Scarab.CustomControls;
using Scarab.Models;
using Scarab.Services;
using Scarab.Util;
using Scarab.Views;
using SkiaSharp;
using Xunit;

namespace Scarab.Tests;

public class MiscServicesTest
{
    /// <summary>
    /// We want to test whether or not scarab can successfully read the mod version from Modding.ModHooks class
    /// the MockMAPI.dll is just a dll with a class in Modding namespace and has a ModHooks class with a
    /// _modversion field
    /// </summary>
    [Fact]
    public void GetAPIVersion()
    {
        var version = Util.CheckValidityOfAssemblies.GetAPIVersion(Directory.GetCurrentDirectory(), "MockMAPI.dll");
        Assert.NotNull(version);
        Assert.Equal(74, version);
    }
    
    /// <summary>
    /// We want to test whether or not scarab can successfully get the global settings file for a mod
    /// first one is exact name, second one is name without spaces, third one is name with mod suffix
    /// fourth one is a mod with different display and mod class name.
    /// The MockModWithDifferentName.dll is just a dll with a class that inherits from Modding.Mod and that
    /// classes name is MockModWithSlightlyDifferentName
    /// </summary>
    [Fact]
    public void FindSettingsFile()
    {
        var savesFolder = Directory.GetCurrentDirectory();
        
        File.WriteAllText(Path.Combine(savesFolder, "MockNormalMod.GlobalSettings.json"), "");
        File.WriteAllText(Path.Combine(savesFolder, "MockModWithModSuffixMod.GlobalSettings.json"), "");
        File.WriteAllText(Path.Combine(savesFolder, "MockModWithSpacesInName.GlobalSettings.json"), "");
        File.WriteAllText(Path.Combine(savesFolder, "MockModWithSlightlyDifferentName.GlobalSettings.json"), "");

        var installedState = new InstalledState(true, new Version(0, 0, 0), true);
        
        var normalMod = ModItem.Empty(name: "MockNormalMod", state: installedState);
        var modWithModSuffix = ModItem.Empty(name: "MockModWithModSuffix", state: installedState);
        var modWithSpacesInName = ModItem.Empty(name: "Mock Mod With Spaces In Name", state: installedState);
        var ModWithDifferentName = ModItem.Empty(name: "MockModWithDifferentName", state: installedState);
        
        //if it returns null it means it didn't find the file
        var gsFinder = new GlobalSettingsFinder(new Settings(Directory.GetCurrentDirectory()));
        Assert.NotNull(gsFinder.GetSettingsFileLocation(normalMod, savesFolder)); 
        Assert.NotNull(gsFinder.GetSettingsFileLocation(modWithModSuffix, savesFolder)); 
        Assert.NotNull(gsFinder.GetSettingsFileLocation(modWithSpacesInName, savesFolder)); 
        Assert.NotNull(gsFinder.GetSettingsFileLocation(ModWithDifferentName, savesFolder));
    }
    
    
    /// <summary>
    /// We do a few reflections to make our flyouts look how we want. The existence of those fields and
    /// properties are not guaranteed and wont fail at compile time so better test here 
    /// </summary>
    [Fact]
    public void ValidReflections()
    {
        // if they are null they dont exist
        Assert.NotNull(TextButtonFlyout.FlyoutBasePopup);
        Assert.NotNull(ModListView.MenuItemPopup);
    }
    
    /// <summary>
    /// Test that each command can actually be parsed properly
    /// </summary>
    [Fact]
    public void UriArgumentParser()
    {
        Assert.Equal(UriCommands.none, UrlSchemeHandler.UriCommand);

        // empty command shouldn't cause it to die
        UrlSchemeHandler.SetCommand("scarab://");
        Assert.Equal(UriCommands.none, UrlSchemeHandler.UriCommand);

        // incorrect command shouldn't cause it to die
        UrlSchemeHandler.SetCommand("scarab://aidhasjkh");
        Assert.Equal(UriCommands.none, UrlSchemeHandler.UriCommand);
        
        // test download
        UrlSchemeHandler.SetCommand("scarab://download/MyMod1");
        Assert.Equal(UriCommands.download, UrlSchemeHandler.UriCommand);
        Assert.Equal("MyMod1", UrlSchemeHandler.Data);
        
        // test download multiple mods
        UrlSchemeHandler.SetCommand("scarab://download/MyMod1/MyMod2");
        Assert.Equal(UriCommands.download, UrlSchemeHandler.UriCommand);
        Assert.Equal("MyMod1/MyMod2", UrlSchemeHandler.Data);
        
        UrlSchemeHandler.SetCommand("scarab://reset");
        Assert.Equal(UriCommands.reset, UrlSchemeHandler.UriCommand);
        
        UrlSchemeHandler.SetCommand("scarab://forceUpdateAll");
        Assert.Equal(UriCommands.forceUpdateAll, UrlSchemeHandler.UriCommand);
        
        UrlSchemeHandler.SetCommand("scarab://customModLinks/https://github.com/SFGrenade/additionalmodlinks/blob/main/ModLinks.xml");
        Assert.Equal(UriCommands.customModLinks, UrlSchemeHandler.UriCommand);
        Assert.Equal("https://github.com/SFGrenade/additionalmodlinks/blob/main/ModLinks.xml", UrlSchemeHandler.Data);
    }
}