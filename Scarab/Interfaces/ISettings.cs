using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Scarab.Models;

namespace Scarab.Interfaces
{
    public interface ISettings
    {
        AutoRemoveUnusedDepsOptions AutoRemoveUnusedDeps { get; set; }
        bool WarnBeforeRemovingDependents { get; set; }

        string ManagedFolder { get; set; }

        bool RequiresWorkaroundClient { get; set; }
        
        string ModsFolder     => Path.Combine(ManagedFolder, "Mods");
        string DisabledFolder => Path.Combine(ModsFolder, "Disabled");

        void Save();
    }
}