using System;
using System.Text.Json.Serialization;
using Mono.Cecil;
using Scarab.Util;

namespace Scarab.Models
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

    public record NotInModLinksState : ExistsModState
    {
        public bool Installed { get; init; }
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