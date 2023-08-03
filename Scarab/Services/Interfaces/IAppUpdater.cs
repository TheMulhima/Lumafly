using System.Threading.Tasks;

namespace Scarab.Interfaces;

public interface IAppUpdater
{
    public Task CheckUpToDate(bool forced = false);
}