namespace Lumafly.Interfaces;

public interface ICheckValidityOfAssembly
{
    public int? GetAPIVersion(string asmName, out string? gameVersion);
    public bool CheckVanillaFileValidity(string vanillaAssembly, string apiAssembly);
}