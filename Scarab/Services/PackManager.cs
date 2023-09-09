using Scarab.Interfaces;
using Scarab.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using Avalonia.Controls;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using Scarab.Util;
using System.IO.Compression;
using Avalonia.Platform.Storage;
using HPackage.Net;
using Scarab.ViewModels;
using Scarab.Views.Windows;

namespace Scarab.Services;

public class PackManager : IPackManager
{
    private readonly ISettings _settings;
    private readonly IInstaller _installer;
    private readonly IFileSystem _fs;
    private readonly IModSource _mods;
    private readonly IOnlineTextStorage _onlineTextStorage;
    /// <summary>
    /// Private collection to allow constant lookup times of ModItems from name.
    /// </summary>
    private readonly Dictionary<string, ModItem> _items;

    /// <summary>
    /// A list of all the profiles saved by the application.
    /// </summary>
    private readonly SortableObservableCollection<Pack> _packList;
    
    public SortableObservableCollection<Pack> PackList => _packList;

    private const string packInfoFileName = "packMods.json";

    public PackManager(ISettings settings, IInstaller installer, IModDatabase db, IFileSystem fs, IModSource mods, IOnlineTextStorage onlineTextStorage)
    {
        _installer = installer;
        _fs = fs;
        _mods = mods;
        _settings = settings;
        _onlineTextStorage = onlineTextStorage;
        
        _items = db.Items.ToDictionary(x => x.Name, x => x);

        _packList = new SortableObservableCollection<Pack>(FindAvailablePacks());
        _packList.SortBy((pack1, pack2) => string.CompareOrdinal(pack1.Name, pack2.Name));
    }

    /// <summary>
    /// Look through the managed folder for any profiles and add them to the list.
    /// </summary>
    private IEnumerable<Pack> FindAvailablePacks()
    {
        var packs = new List<Pack>();
        
        var packFolderLocation = _settings.ManagedFolder;

        foreach (var folderPath in _fs.Directory.EnumerateDirectories(packFolderLocation))
        {
            var folder = Path.GetFileName(folderPath);
            if (folder == "Mods") continue; // ignore mod folder
            
            var packInfo = Path.Combine(folderPath, packInfoFileName);
            if (!_fs.File.Exists(packInfo)) continue;

            try
            {
                var pack = JsonSerializer.Deserialize<Pack>(File.ReadAllText(packInfo));
                if (pack?.Name == folder) // only add packs with same name as folder
                    packs.Add(pack);
            }
            catch (Exception e)
            {
                Trace.TraceError($"Error reading pack info file {e}");
            }
        }

        return packs;
    }

    /// <summary>
    /// Loads a pack as the main mod list.
    /// </summary>
    /// <param name="packName">The profile to set as current.</param>
    public async Task<bool> LoadPack(string packName)
    {
        await EnsureGameClosed();
        
        var pack = _packList.FirstOrDefault(x => x.Name == packName);
        if (pack == null)
        {
            await DisplayErrors.DisplayGenericError("Could not set current profile: profile not found!");
            return false;
        }

        var packFolder = Path.Combine(_settings.ManagedFolder, packName);

        // move all the mods to a temp folder so we can revert if the pack enabling fails
        FileUtil.CopyDirectory(_settings.ModsFolder, Path.Combine(_settings.ManagedFolder, "Temp_Mods_Storage"));
        FileUtil.DeleteDirectory(_settings.ModsFolder);
        var tempDbMods = _mods.Mods.ToDictionary(x => x.Key, x => x.Value);
        var tempDbNotInModlinksMods = _mods.NotInModlinksMods.ToDictionary(x => x.Key, x => x.Value);
        
        // enable pack
        try
        {
            // move the mods from the profile to the mods folder
            _fs.Directory.Move(packFolder, _settings.ModsFolder);
            await _mods.SetMods(pack.InstalledMods.Mods, pack.InstalledMods.NotInModlinksMods);

            var requestedModList = _mods.Mods.Select(x => x.Key)
                .Concat(_mods.NotInModlinksMods.Select(x => x.Key))
                .ToArray();
            
            // get the full dependency list of the pack
            HashSet<string> fullDependencyList = new();

            // ensure all mods that are supposed to be in the pack are installed
            foreach (string modName in requestedModList)
            {
                if (InstalledMods.ModExists(_settings, modName, out _))
                    continue;
                
                if (_items.TryGetValue(modName, out var correspondingMod))
                {
                    // to ensure the mod is installed when OnInstall is called
                    correspondingMod.State = new NotInstalledState();
                    await _installer.Install(correspondingMod, _ => { }, true);

                    // save full dependency list which will be used to decide what to uninstall
                    foreach (var dep in correspondingMod.Dependencies)
                        AddDependency(fullDependencyList, dep);
                    
                }
                else // can't install so yeet it from the profile
                {
                    _mods.Mods.Remove(modName);
                    _mods.NotInModlinksMods.Remove(modName);
                }
            }

            var currentList = _fs.Directory.EnumerateDirectories(_settings.ModsFolder).ToList();
            
            if (_fs.Directory.Exists(_settings.DisabledFolder))
                currentList.AddRange(_fs.Directory.EnumerateDirectories(_settings.DisabledFolder));

            // uninstall mods that arent in pack
            foreach (var modNamePath in currentList)
            {
                var modName = Path.GetFileName(modNamePath);
                if (modName == "Disabled") continue; // skip disabled Folder
                
                // don't uninstall mods that are said to be in the pack
                if (_mods.NotInModlinksMods.ContainsKey(modName)) continue;
                if (_mods.Mods.ContainsKey(modName)) continue;

                // don't uninstall if the mod is a dependency of a pack mod
                if (fullDependencyList.Contains(modName)) continue;
                
                // since it isn't a listed pack mod or a dependency, uninstall it
                FileUtil.DeleteDirectory(Path.Combine(_settings.ModsFolder, modName));
                FileUtil.DeleteDirectory(Path.Combine(_settings.DisabledFolder, modName));
            }
            
            // cache result so it is easier to load next time
            await SavePack(pack.Name, pack.Description, pack.Authors);
            
            _fs.File.Delete(Path.Combine(_settings.ModsFolder, packInfoFileName));
            
            // delete temp folder
            _fs.Directory.Delete(Path.Combine(_settings.ManagedFolder, "Temp_Mods_Storage"), true);

            return true;
        }
        catch (Exception e)
        {
            _fs.Directory.Delete(_settings.ModsFolder, true);
            _fs.Directory.Move(Path.Combine(_settings.ManagedFolder, "Temp_Mods_Storage"), _settings.ModsFolder);
            await _mods.SetMods(tempDbMods, tempDbNotInModlinksMods);
            
            await DisplayErrors.DisplayGenericError("An error occured when activating pack. Scarab will revert to old mods", e);

            return false;
        }
    }

    /// <summary>
    /// Saves the current Mods folder as a pack. Replaces the pack if it already exists.
    /// </summary>
    public async Task SavePack(string name, string description, string authors)
    {
        var packFolder = Path.Combine(_settings.ManagedFolder, name);

        FileUtil.DeleteDirectory(packFolder);
        
        FileUtil.CopyDirectory(_settings.ModsFolder, packFolder, excludeDir: "Disabled");

        var packInfo = Path.Combine(packFolder, packInfoFileName);
        
        var packDetails = new Pack(name, description, authors, new InstalledMods()
        {
            Mods = _mods.Mods.Where(x => x.Value.Enabled).ToDictionary(x => x.Key, x => x.Value),
            NotInModlinksMods = _mods.NotInModlinksMods.Where(x => x.Value.Enabled).ToDictionary(x => x.Key, x => x.Value),
        });
        
        await using Stream fs = _fs.File.Exists(packInfo)
            ? _fs.FileStream.New(packInfo, FileMode.Truncate)
            : _fs.File.Create(packInfo);

        await JsonSerializer.SerializeAsync(fs, packDetails, new JsonSerializerOptions() 
        { 
            WriteIndented = true
        });
        
        var packInList = PackList.FirstOrDefault(x => x.Name == name);
        if (packInList != null) // if the pack already exists, replace it
            packInList.InstalledMods = packDetails.InstalledMods;
        else // otherwise add it to the list
            PackList.Add(packDetails); 
        
        PackList.SortBy((pack1, pack2) => string.CompareOrdinal(pack1.Name, pack2.Name));
    }
    
    /// <summary>
    /// Saves the current instance of <paramref name="pack"/> to disk
    /// </summary>
    public async Task SaveEditedPack(Pack pack, string? oldPackName = null)
    {
        // if the name changed, move the folder to reflect the change
        if (oldPackName != null && oldPackName != pack.Name)
        {
            Directory.Move(Path.Combine(_settings.ManagedFolder, oldPackName), Path.Combine(_settings.ManagedFolder, pack.Name));
        }

        // write the new pack info to disk
        var packFolder = Path.Combine(_settings.ManagedFolder, pack.Name);

        FileUtil.CreateDirectory(packFolder);
        
        var packInfo = Path.Combine(packFolder, packInfoFileName);
        
        await using Stream fs = _fs.File.Exists(packInfo)
            ? _fs.FileStream.New(packInfo, FileMode.Truncate)
            : _fs.File.Create(packInfo);

        await JsonSerializer.SerializeAsync(fs, pack, new JsonSerializerOptions() 
        { 
            WriteIndented = true
        });
        
        PackList.SortBy((pack1, pack2) => string.CompareOrdinal(pack1.Name, pack2.Name));
    }

    /// <summary>
    /// Remove a profile by object.
    /// </summary>
    /// <param name="packName">The pack to remove.</param>
    /// <exception cref="ArgumentException">The profile to remove was not found in the profile list.</exception>
    public void RemovePack(string packName)
    {
        var pack = _packList.FirstOrDefault(x => x.Name == packName);
        if (pack != null)
        {
            _packList.Remove(pack);
        }

        FileUtil.DeleteDirectory(Path.Combine(_settings.ManagedFolder, packName));
    }
    
    /// <summary>
    /// Adds a mods full dependency tree to the dependency list.
    /// </summary>
    private void AddDependency(ISet<string> fullDependencyList, string depName)
    {
        // so we don't unnecessarily go through the dependency list
        if (fullDependencyList.Contains(depName)) return;
                        
        fullDependencyList.Add(depName);
                        
        // add all of its dependency to the dependency list
        if (_items.TryGetValue(depName, out var depMod))
        {
            foreach (var dep in depMod.Dependencies)
            {
                AddDependency(fullDependencyList, dep);
            }
        }
    }

    private async Task EnsureGameClosed()
    {
        static bool IsHollowKnight(Process p) => (
            p.ProcessName.StartsWith("hollow_knight")
            || p.ProcessName.StartsWith("Hollow Knight")
        );
            
        if (Process.GetProcesses().FirstOrDefault(IsHollowKnight) is { } proc)
        {
            var res = await MessageBoxUtil.GetMessageBoxStandardWindow(new MessageBoxStandardParams {
                ContentTitle = Resources.MLVM_InternalUpdateInstallAsync_Msgbox_W_Title,
                ContentMessage = Resources.MLVM_InternalUpdateInstallAsync_Msgbox_W_Text,
                ButtonDefinitions = ButtonEnum.YesNo,
                MinHeight = 200,
                SizeToContent = SizeToContent.WidthAndHeight,
            }).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());

            if (res == ButtonResult.Yes)
                proc.Kill();
        }
    }

    /// <summary>
    /// Create a .zip file containing the pack in location specified by user
    /// </summary>
    /// <param name="packName"></param>
    public async void SavePackToZip(string packName)
    {
        await EnsureGameClosed();

        var pack = _packList.FirstOrDefault(x => x.Name == packName);
        if (pack == null)
        {
            await DisplayErrors.DisplayGenericError("Could not share profile: profile not found!");
            return;
        }

        var packFolder = Path.Combine(_settings.ManagedFolder, packName);

        TopLevel? topLevel = TopLevel.GetTopLevel(AvaloniaUtils.GetMainWindow()); // Necessary for save file picker
        if (topLevel == null) return;

        var options = new FilePickerSaveOptions
        {
            Title = "Select save location",
            ShowOverwritePrompt = true,
            DefaultExtension = "zip",
            SuggestedFileName = packName,
            FileTypeChoices = new List<FilePickerFileType>()
            {
                new ("ZIP Archive")
                {
                    Patterns = new List<string>() { "*.zip" }
                }
            }
        };

        // This crashes on multiple repeated attempts due to avalonia issue
        IStorageFile? storage_file = await topLevel.StorageProvider.SaveFilePickerAsync(options);

        string? outputFilePath = storage_file?.TryGetLocalPath();
        if (outputFilePath == null) return; // Couldn't get local path for some reason

        CreateZip(packFolder, outputFilePath);
    }

    public static Window? CurrentPackWindow;
    
    /// <summary>
    /// Opens a window to edit pack details
    /// </summary>
    /// <param name="pack">The pack to edit</param>
    public async Task EditPack(Pack pack)
    {
        string oldPackName = pack.Name;
        Pack copiedPack = pack.DeepCopy();

        CurrentPackWindow = new EditPackWindow
        {
            DataContext = new EditPackWindowViewModel(copiedPack, _items.Values)
        };
        
        var shouldSave = await CurrentPackWindow.ShowDialog<bool>(AvaloniaUtils.GetMainWindow());

        if (shouldSave)
        {
            pack.Copy(copiedPack);
            if (!string.IsNullOrEmpty(pack.SharingCode) && pack.IsSame(copiedPack)) 
                pack.SharingCode = null;
            await SaveEditedPack(copiedPack, oldPackName);
        }
    }

    /// <summary>
    /// Create a .zip file from a directory. Returns if the creation was successful
    /// </summary>
    /// <param name="sourcePath">Path to the directory to be zipped</param>
    /// <param name="outputFilePath">Path to the new .zip file</param>
    private bool CreateZip(string sourcePath, string outputFilePath)
    {
        try
        {
            if (!Directory.Exists(sourcePath))
            {
                throw new DirectoryNotFoundException("Couldn't find the source directory");
            }

            ZipFile.CreateFromDirectory(sourcePath, outputFilePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task UploadPack(string packName)
    {
        var pack = _packList.FirstOrDefault(x => x.Name == packName);
        if (pack == null) return;

        try
        {
            var code = await _onlineTextStorage.Upload(packName, ConvertToHPackage(pack));
            pack.SharingCode = code.Replace("https://pastebin.com/", "");
            await SaveEditedPack(pack);
        }
        catch (Exception e)
        {
            await DisplayErrors.DisplayGenericError("Failed to upload pack", e);
        }
    }
    
    public async Task<Pack?> ImportPack(string code)
    {
        try
        {
            var packJson = await _onlineTextStorage.Download(code);
            
            var pack = ConvertFromHPackage(packJson);
            pack.SharingCode = code; //since we have it lets save it

            PackList.Add(pack);
            await SaveEditedPack(pack);
            
            return pack;
        }
        catch (Exception e)
        {
            await DisplayErrors.DisplayGenericError("Failed to import pack", e);
            return null;
        }
    }

    private string ConvertToHPackage(Pack pack)
    {
        var packageDef = new HollowKnightPackageDef()
        {
            Name = pack.Name,
            Description = pack.Description,
            Authors = new List<string> { pack.Authors },
            Dependencies = new References()
            {
                AnythingMap = pack.InstalledMods.Mods.ToDictionary(
                    x => x.Key, 
                    x => new ReferenceVersion() 
                    { 
                        String = JsonSerializer.Serialize(x.Value) 
                    })
            }
        };

        return packageDef.ToJson();
    }
    
    private Pack ConvertFromHPackage(string json)
    {
        var packageDef = HollowKnightPackageDef.FromJson(json);

        var pack = new Pack(
            packageDef.Name,
            packageDef.Description,
            string.Join(',', packageDef.Authors),
            new InstalledMods()
            {
                Mods = packageDef.Dependencies!.Value.AnythingMap.ToDictionary(
                    x => x.Key,
                    x => JsonSerializer.Deserialize<InstalledState>(x.Value.String)!)
            });
        
        return pack;
    }
}