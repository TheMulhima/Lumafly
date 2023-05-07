using System.Collections.Generic;
using Scarab.Models;

namespace Scarab.Interfaces;

public interface IReverseDependencySearch
{
    public IEnumerable<ModItem> GetAllEnabledDependents(ModItem item);
    public IEnumerable<ModItem> GetAllDependentAndIntegratedMods(ModItem item);
}