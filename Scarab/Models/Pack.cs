using System;
using System.Linq;
using Scarab.Services;

namespace Scarab.Models;
/// <summary>
/// A record to represent a pack of mods
/// </summary>
[Serializable]
public record Pack(string Name, string Description, string Authors, InstalledMods InstalledMods)
{
    /// <summary>
    /// A list of the names of the mods in the profile.
    /// </summary>
    public InstalledMods InstalledMods { get; set; } = InstalledMods;
    
    /// <summary>
    /// The name of the pack
    /// </summary>
    public string Name { get; set; } = Name;
    
    /// <summary>
    /// The description of the pack
    /// </summary>
    public string Description { get; set; } = Description;
    
    /// <summary>
    /// The description of the pack
    /// </summary>
    public string Authors { get; set; } = Authors;

    public string? SharingCode { get; set; }
    
    public bool HasSharingCode => !string.IsNullOrEmpty(SharingCode);
    
    public string ModList => InstalledMods.Mods.Keys
        .Concat(InstalledMods.NotInModlinksMods.Keys.Select(x => $"{x} ({Resources.MVVM_NotInModlinks_Disclaimer})"))
        .Aggregate("", (x, y) => x + y + "\n");
    
    public Pack DeepCopy() => new(Name, Description, Authors, InstalledMods.DeepCopy());

    public void Copy(Pack pack)
    {
        Name = pack.Name;
        Description = pack.Description;
        Authors = pack.Authors;
        InstalledMods = pack.InstalledMods;
    }
}