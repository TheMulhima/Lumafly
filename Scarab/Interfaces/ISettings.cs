using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Scarab.Models;
using Scarab.Services;

namespace Scarab.Interfaces
{
    public interface ISettings
    {
        AutoRemoveUnusedDepsOptions AutoRemoveUnusedDeps { get; set; }
        bool WarnBeforeRemovingDependents { get; set; }
        bool UseCustomModlinks { get; set; }
        string CustomModlinksUri { get; set; }
        string BaseLink { get; set; }

        string ManagedFolder { get; set; }

        bool RequiresWorkaroundClient { get; set; }
        
        string ModsFolder     => Path.Combine(ManagedFolder, "Mods");
        string DisabledFolder => Path.Combine(ModsFolder, "Disabled");

        void Save();
    }
}