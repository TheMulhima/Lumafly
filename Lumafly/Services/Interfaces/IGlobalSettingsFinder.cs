using Lumafly.Models;

namespace Lumafly.Interfaces;

public interface IGlobalSettingsFinder
{
    public string? GetSettingsFileLocation(ModItem modItem);
}