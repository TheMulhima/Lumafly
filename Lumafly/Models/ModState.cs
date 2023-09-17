using System;
using System.Text.Json.Serialization;
using Mono.Cecil;
using Lumafly.Util;

namespace Lumafly.Models
{
    public abstract record ModState;

    public abstract record ExistsModState : ModState
    {
        public bool Enabled { get; init; }

        public bool Pinned { get; init; }
        
        [JsonIgnore]
        public bool Updating { get; init; }
    }

    public record InstalledState : ExistsModState
    {
        [JsonConverter(typeof(JsonVersionConverter))]
        public Version Version { get; init; }
        
        [JsonIgnore]
        public bool Updated { get; init; }

        public InstalledState(bool Enabled, Version Version, bool Updated, bool Pinned = false)
        {
            this.Enabled = Enabled;
            this.Version = Version;
            this.Updated = Updated;
            this.Pinned = Pinned;
        }
    }

    public record NotInstalledState(bool Installing = false) : ModState;

    /// <summary>
    /// Mods that are not installed, but are in modlinks.xml file.
    /// </summary>
    public record NotInModLinksState : ExistsModState
    {
        /// <summary>
        /// A helper bool to indicate whether a custom mod has been uninstalled and needs to be removed from the list
        /// Defaults to always being true except after being uninstalled.
        /// </summary>
        public bool Installed { get; init; }
        
        /// <summary>
        /// This specifies whether or not the mod has a corresponding modlinks entry.
        /// If set to false, this is a completely custom mod.
        /// If set to true, this is a mod that is in modlinks but is currently a custom non modlinks version of it is installed
        /// </summary>
        public bool ModlinksMod { get; init; }

        public NotInModLinksState(bool ModlinksMod, bool Enabled = true, bool Installed = true, bool Pinned = false)
        {
            this.Enabled = Enabled;
            this.Installed = Installed;
            this.ModlinksMod = ModlinksMod;
            this.Pinned = Pinned;
        }
    }
}