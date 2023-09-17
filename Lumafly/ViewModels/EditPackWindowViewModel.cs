using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using Lumafly.Models;
using Lumafly.Services;
using Lumafly.Util;

namespace Lumafly.ViewModels;

public partial class EditPackWindowViewModel : ViewModelBase
{
    private readonly Pack _pack;
    private string _modSearch = "";
    private readonly SortableObservableCollection<SelectableItem<ModItem>> _mods;
    private readonly Dictionary<string, ModItem> _modLookup;

    public IEnumerable<ModItem> SelectedMods_Enabled_Modlinks => _pack.InstalledMods.Mods
        .Where(name_state => name_state.Value is { Enabled: true })
        .Select(name_state => _modLookup[name_state.Key]);
    public IEnumerable<ModItem> SelectedMods_Disabled_Modlinks => _pack.InstalledMods.Mods
        .Where(name_state => name_state.Value is { Enabled: false })
        .Select(name_state => _modLookup[name_state.Key]);
    
    public IEnumerable<ModItem> SelectedMods_Enabled_NotModlinks => _pack.InstalledMods.NotInModlinksMods
        .Where(name_state => name_state.Value is { Enabled: true })
        .Select(name_state => _modLookup[name_state.Key]);
    
    public IEnumerable<ModItem> SelectedMods_Disabled_NotModlinks => _pack.InstalledMods.NotInModlinksMods
        .Where(name_state => name_state.Value is { Enabled: false })
        .Select(name_state => _modLookup[name_state.Key]);
    
    public IEnumerable<SelectableItem<ModItem>> FilteredMods => _mods
        .Where(x => x.DisplayName.Contains(ModSearch, StringComparison.OrdinalIgnoreCase));

    public EditPackWindowViewModel(Pack pack, IEnumerable<ModItem> mods)
    {
        _pack = pack;
        var sortedModList = mods.ToList();
        
        // we need to re sort mods so that pinned or outdated mods aren't on top
        sortedModList.Sort((x,y) => string.CompareOrdinal(x.Name, y.Name));
        
        _mods = new SortableObservableCollection<SelectableItem<ModItem>>(sortedModList.Select(x => 
            new SelectableItem<ModItem>(x, x.Name, 
            IsSelected: pack.InstalledMods.Mods.ContainsKey(x.Name) || pack.InstalledMods.NotInModlinksMods.ContainsKey(x.Name))));

        _modLookup = _mods.ToDictionary(x => x.Item.Name, x => x.Item);
    }
    
    public void ModSelectionChanged(object? modObj)
    {
        if (modObj is not null)
        {
            var mod = (SelectableItem<ModItem>)modObj;

            if (mod.IsSelected)
            {
                var fixedVersion = mod.Item.State is NotInModLinksState;
                var modlinksMod = mod.Item.State is not NotInModLinksState { ModlinksMod: false };
                var enabled = mod.Item.State is ExistsModState { Enabled: true } or NotInstalledState;

                if (!fixedVersion)
                    _pack.InstalledMods.Mods.Add(mod.Item.Name,
                        new InstalledState(enabled, mod.Item.Version, !mod.Item.UpdateAvailable));
                else
                    _pack.InstalledMods.NotInModlinksMods.Add(mod.Item.Name,
                        new NotInModLinksState(modlinksMod, enabled));
            }
            else
            {
                _pack.InstalledMods.Mods.Remove(mod.Item.Name);
                _pack.InstalledMods.NotInModlinksMods.Remove(mod.Item.Name);
            }
        }

        RaisePropertyChanged(nameof(SelectedMods_Enabled_Modlinks));
        RaisePropertyChanged(nameof(SelectedMods_Disabled_Modlinks));
        RaisePropertyChanged(nameof(SelectedMods_Enabled_NotModlinks));
        RaisePropertyChanged(nameof(SelectedMods_Disabled_NotModlinks));
    }

    public void UseModlinksVersion(object modObj)
    {
        var mod = (ModItem) modObj;
        
        var modLinksMod = _modLookup[mod.Name].State is not NotInModLinksState { ModlinksMod: false };
        if (!modLinksMod)
        {
            Dispatcher.UIThread.Invoke(async () => await MessageBoxUtil
                .GetMessageBoxStandardWindow("Warning", "This mod is not available on modlinks.")
                .ShowAsPopupAsync(PackManager.CurrentPackWindow));
            return;
        }
        
        var enabled = _pack.InstalledMods.NotInModlinksMods[mod.Name].Enabled;
        
        _pack.InstalledMods.Mods.Add(mod.Name, new InstalledState(enabled, mod.Version, false));
        _pack.InstalledMods.NotInModlinksMods.Remove(mod.Name);
        
        ModSelectionChanged(null);
    }
    
    public void UseFixedVersion(object modObj)
    {
        var mod = (ModItem) modObj;
        
        var enabled = _pack.InstalledMods.Mods[mod.Name].Enabled;
        
        _pack.InstalledMods.NotInModlinksMods.Add(mod.Name, new NotInModLinksState(true, enabled));
        _pack.InstalledMods.Mods.Remove(mod.Name); 
        
        ModSelectionChanged(null);
    }
    
    public void EnableMod(object modObj)
    {
        var mod = (ModItem) modObj;

        if (_pack.InstalledMods.Mods.ContainsKey(mod.Name))
            _pack.InstalledMods.Mods[mod.Name] = _pack.InstalledMods.Mods[mod.Name] with { Enabled = true };
        else
            _pack.InstalledMods.NotInModlinksMods[mod.Name] =
                _pack.InstalledMods.NotInModlinksMods[mod.Name] with { Enabled = true };
        
        ModSelectionChanged(null);
    }
    
    public void DisableMod(object modObj)
    {
        var mod = (ModItem) modObj;

        if (_pack.InstalledMods.Mods.ContainsKey(mod.Name))
            _pack.InstalledMods.Mods[mod.Name] = _pack.InstalledMods.Mods[mod.Name] with { Enabled = false };
        else
            _pack.InstalledMods.NotInModlinksMods[mod.Name] =
                _pack.InstalledMods.NotInModlinksMods[mod.Name] with { Enabled = false };
        
        ModSelectionChanged(null);
    }
    
    public string ModSearch
    {
        get => _modSearch;
        set
        {
            if (_modSearch == value) return;
            
            _modSearch = value;
            RaisePropertyChanged(nameof(ModSearch));
            RaisePropertyChanged(nameof(FilteredMods));
        }
    }

    public string PackName
    {
        get => _pack.Name;
        set
        {
            _pack.Name = value;
            RaisePropertyChanged(nameof(PackName));
        }
    }    
    public string PackDescription
    {
        get => _pack.Description;
        set
        {
            _pack.Description = value;
            RaisePropertyChanged(nameof(PackDescription));
        }
    }
    public string PackAuthors
    {
        get => _pack.Authors;
        set
        {
            _pack.Authors = value;
            RaisePropertyChanged(nameof(PackAuthors));
        }
    }
}