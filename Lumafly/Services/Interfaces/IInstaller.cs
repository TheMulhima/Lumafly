using System;
using System.Threading.Tasks;
using Lumafly.Models;

namespace Lumafly.Interfaces
{
    public interface IInstaller
    {
        public Task Toggle(ModItem mod);

        public Task Install(ModItem mod, Action<ModProgressArgs> setProgress, bool enable, bool clearDlls = false);

        public Task PlaceMod(ModItem mod, bool enable, string filename, ArraySegment<byte> data);

        public Task Uninstall(ModItem mod);

        public Task InstallApi();

        public Task InstallVanilla();

        public Task ToggleApi();
        
        public Task<bool> CheckAPI();

        public Task Pin(ModItem mod, bool pinned);
    }
}