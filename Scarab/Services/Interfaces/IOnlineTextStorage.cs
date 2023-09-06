using System.Threading.Tasks;

namespace Scarab.Interfaces;

/// <summary>
/// Interface for online text storage services. Used for sharing mod packs
/// </summary>
public interface IOnlineTextStorage
{
    public Task<string> Upload(string name, string data);
    public Task<string> Download(string code);
}