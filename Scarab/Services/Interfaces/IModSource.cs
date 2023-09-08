using System.Collections.Generic;
using System.Threading.Tasks;
using Scarab.Models;

namespace Scarab.Interfaces
{
    public interface IModSource
    {
        ModState ApiInstall { get; }

        Dictionary<string, InstalledState> Mods { get; }
        
        Dictionary<string, NotInModLinksState> NotInModlinksMods { get; }

        Task RecordApiState(ModState st);

        ModState FromManifest(Manifest manifest);

        Task RecordInstalledState(ModItem item);

        Task RecordUninstall(ModItem item);

        Task Reset();

        Task SetMods(
            Dictionary<string, InstalledState> mods,
            Dictionary<string, NotInModLinksState> notInModlinksMods);
        
        bool HasVanilla { get; set; }
    }
}