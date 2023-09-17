using System.Threading.Tasks;

namespace Lumafly.Interfaces;

public interface IAppUpdater
{
    public Task CheckUpToDate(bool forced = false);
}