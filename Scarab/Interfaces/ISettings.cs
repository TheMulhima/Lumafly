using System.IO;

namespace Scarab.Interfaces
{
    public interface ISettings
    {
        bool AutoRemoveDeps { get; }
        
        string ManagedFolder { get; set; }
        
        bool RequiresWorkaroundClient { get; set; }
        
        string ModsFolder     => Path.Combine(ManagedFolder, "Mods");
        string DisabledFolder => Path.Combine(ModsFolder, "Disabled");

        void Save();
    }
}