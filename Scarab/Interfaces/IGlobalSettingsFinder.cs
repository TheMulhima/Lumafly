using Scarab.Models;

namespace Scarab.Interfaces;

public interface IGlobalSettingsFinder
{
    public string? GetSettingsFileLocation(ModItem modItem);
}