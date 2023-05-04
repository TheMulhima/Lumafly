using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Scarab.Models;

namespace Scarab.Interfaces
{
    public interface ISettings
    {
        bool AutoRemoveDeps { get; }
        
        string ManagedFolder { get; set; }

        string? CurrentProfileName { get; set; }

        ObservableCollection<Profile> Profiles { get; set; }

        bool RequiresWorkaroundClient { get; set; }
        
        string ModsFolder     => Path.Combine(ManagedFolder, "Mods");
        string DisabledFolder => Path.Combine(ModsFolder, "Disabled");

        void Save();
    }
}