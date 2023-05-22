using System.Threading.Tasks;

namespace Scarab.Interfaces;

public interface IModLinksChanges
{
    public Task LoadChanges();
    public bool? IsReady { get; set; }
}