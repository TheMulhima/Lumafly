using Lumafly.Enums;
using System.IO;

namespace Lumafly.Interfaces
{
    public interface ISettings
    {
        AutoRemoveUnusedDepsOptions AutoRemoveUnusedDeps { get; set; }
        bool WarnBeforeRemovingDependents { get; set; }
        bool UseCustomModlinks { get; set; }
        string CustomModlinksUri { get; set; }
        SupportedLanguages? PreferredLanguage { get; set; }
        bool LowStorageMode { get; set; }
        string ExtraSpaceTaken { get; }

        string ManagedFolder { get; set; }
        string CacheFolder { get; }

        bool RequiresWorkaroundClient { get; set; }
        
        string ModsFolder     => Path.Combine(ManagedFolder, "Mods");
        string DisabledFolder => Path.Combine(ModsFolder, "Disabled");

        string GithubMirrorFormat { get; set; }
        bool UseGithubMirror { get; set; }

        void Save();
    }
}