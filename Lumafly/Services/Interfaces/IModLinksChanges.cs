using System.Threading.Tasks;

namespace Lumafly.Interfaces;

public interface IModLinksChanges
{
    public Task LoadChanges();
    public bool? IsLoaded { get; }
}