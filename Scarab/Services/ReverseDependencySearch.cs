using System.Collections.Generic;
using System.Linq;
using Scarab.Interfaces;
using Scarab.Models;

namespace Scarab.Services;

public class ReverseDependencySearch : IReverseDependencySearch
{
    // a dictionary to allow constant lookup times of ModItems from name
    private readonly Dictionary<string, ModItem> _items;

    public ReverseDependencySearch(IEnumerable<ModItem> allModItems)
    {
        // no need to add non modlinks mod because they dont have a dependency tree
        _items = allModItems.Where(x => x.State is not NotInModLinksState { ModlinksMod: false })
            .ToDictionary(x => x.Name, x => x);
    }

    public IEnumerable<ModItem> GetAllDependentAndIntegratedMods(ModItem item)
    {
        var dependants = new List<ModItem>();
        
        foreach (var mod in _items.Values)
        {
            if (mod.HasIntegrations)
            {
                if (mod.Integrations.Contains(item.Name))
                {
                    dependants.Add(mod);
                }
            }
            
            if (IsDependent(mod, item))
            {
                dependants.Add(mod);
            }
        }
        return dependants;
    }
    
    public IEnumerable<ModItem> GetAllEnabledDependents(ModItem item)
    {
        // check all enabled mods if they have a dependency on this mod
        return _items.Values.Where(mod => mod.EnabledIsChecked && IsDependent(mod, item));
    }

    private bool IsDependent(ModItem mod, ModItem targetMod)
    {
        foreach (var dependency in mod.Dependencies.Where(x => _items.ContainsKey(x)).Select(x => _items[x]))
        {
            // if the mod's listed dependency is the targetMod, it is a dependency
            if (dependency == targetMod) 
                return true;

            // it also is a dependent if it has a transitive dependent
            if (IsDependent(dependency, targetMod)) 
                return true;
        }

        return false;
    }
}