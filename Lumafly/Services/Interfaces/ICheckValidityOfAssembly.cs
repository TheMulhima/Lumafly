namespace Lumafly.Interfaces;

public interface ICheckValidityOfAssembly
{
    public int? GetAPIVersion(string asmName);
    public bool CheckVanillaFileValidity(string vanillaAssembly);
}