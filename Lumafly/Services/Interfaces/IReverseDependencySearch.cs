using System.Collections.Generic;
using Lumafly.Models;

namespace Lumafly.Interfaces;

public interface IReverseDependencySearch
{
    public IEnumerable<ModItem> GetAllEnabledDependents(ModItem item);
    public IEnumerable<ModItem> GetAllDependentAndIntegratedMods(ModItem item);
    public IEnumerable<ModItem> GetAllIntegratedMods(ModItem item);
}