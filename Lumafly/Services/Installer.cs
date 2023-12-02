using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using MsBox.Avalonia.Enums;
using Microsoft.Toolkit.HighPerformance;
using Lumafly.Interfaces;
using Lumafly.Models;
using Lumafly.Util;

namespace Lumafly.Services
{
    public class HashMismatchException : Exception
    {
        /// <summary>
        /// The SHA256 value that was received
        /// </summary>
        public string Actual { get; }

        /// <summary>
        /// Expected SHA256 value
        /// </summary>
        public string Expected { get; }
        
        /// <summary>
        ///  The name of the object being checked
        /// </summary>
        public string Name { get; }
        
        public HashMismatchException(string name, string actual, string expected)
        {
            Name = name;
            Actual = actual;
            Expected = expected;
        }

    }
    
    public class Installer : IInstaller
    {
        private enum Update
        {
            ForceUpdate,
            LeaveUnchanged
        }

        private readonly ISettings _config;
        private readonly IModSource _installed;
        private readonly IModDatabase _db;
        private readonly IFileSystem _fs;
        private readonly ICheckValidityOfAssembly _checkValidityOfAssembly;
        
        // If we're going to have one be internal, might as well be consistent
        // ReSharper disable MemberCanBePrivate.Global 
        internal const string Modded = "Assembly-CSharp.dll.m";
        internal const string Vanilla = "Assembly-CSharp.dll.v";
        internal const string Current = "Assembly-CSharp.dll";
        // ReSharper restore MemberCanBePrivate.Global

        private readonly SemaphoreSlim _semaphore = new (1);
        // special semaphore for mod toggle because it can occur in concurrently to other installer actions but
        // 2 mod toggles shouldn't happen at the same time because if they are conflicting it can cause problems
        private readonly SemaphoreSlim _modToggleSemaphore = new (1);
        private readonly HttpClient _hc;
        private readonly ISettings _settings;

        public Installer(
            ISettings config, 
            IModSource installed, 
            IModDatabase db,
            IFileSystem fs,
            HttpClient hc,
            ISettings settings,
            ICheckValidityOfAssembly checkValidityOfAssembly)
        {
            _config = config;
            _installed = installed;
            _db = db;
            _fs = fs;
            _hc = hc;
            _settings = settings;
            _checkValidityOfAssembly = checkValidityOfAssembly;

            // run some tasks on installer init
            Task.Run(async () => 
            {
                try
                {
                    if (_config.LowStorageMode && _fs.Directory.Exists(_config.CacheFolder)) 
                        _fs.Directory.Delete(_config.CacheFolder, true);
                    
                    await CheckAPI();
                    Trace.WriteLine("Initial API check complete");
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Exception occured when checking api in init of Installer {e}");
                }
            });
            Trace.WriteLine($"Installer initialized");
        }

        private void CreateNeededDirectories()
        {
            // These both no-op if the directory already exists,
            // so no need to check ourselves
            _fs.Directory.CreateDirectory(_config.DisabledFolder);

            _fs.Directory.CreateDirectory(_config.ModsFolder);
        }
        
        public async Task Pin(ModItem mod, bool pinned)
        {
            if (mod.State is not ExistsModState state)
                throw new InvalidOperationException("Cannot pin mod which is not installed!");

            mod.State = state with { Pinned = pinned };
            await _installed.RecordInstalledState(mod);
        }

        public async Task Toggle(ModItem mod)
        {
            if (mod.State is not ExistsModState state)
                throw new InvalidOperationException("Cannot enable mod which is not installed!");

            var enabled = state.Enabled;

            // Enable dependents when enabling a mod
            if (!enabled)
            {
                foreach (ModItem dep in mod.Dependencies.Select(x => _db.Items.First(i => i.Name == x)))
                {
                    if (dep.State is ExistsModState { Enabled: true } or NotInstalledState)
                        continue;

                    await Toggle(dep);
                }
            }
            
            await _modToggleSemaphore.WaitAsync();

            try
            {
                await _Toggle(mod, enabled, state);
            }
            finally
            {
                _modToggleSemaphore.Release();
            }

        }

        private async Task _Toggle(ModItem mod, bool enabled, ExistsModState state)
        {
            CreateNeededDirectories();

            var (prev, after) = !enabled
                ? (_config.DisabledFolder, _config.ModsFolder)
                : (_config.ModsFolder, _config.DisabledFolder);

            (prev, after) = (
                Path.Combine(prev, mod.Name),
                Path.Combine(after, mod.Name)
            );

            // just in case the mod was deleted or manually moved
            if (!_fs.Directory.Exists(prev))
                throw new ReadableError("because it doesn't exist where it is supposed to. Please try to reinstall the mod");

            // just in case the destination already has same folder 
            if (_fs.Directory.Exists(after))
            {
                // ask the user if they want to delete the existing mod
                if (await ShouldStopToggle(mod, enabled)) return;
                
                _fs.Directory.Delete(after, true);
            }

            // the actual toggling
            _fs.Directory.Move(prev, after);

            // just in case there is left overs
            FileUtil.DeleteDirectory(prev);

            mod.State = state with { Enabled = !state.Enabled };


            await _installed.RecordInstalledState(mod);
        }

        private static async Task<bool> ShouldStopToggle(ModItem mod, bool enabled)
        {
            var destination = enabled ? "Disabled" : "Mods";
            bool shouldQuit = false;
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var res = await MessageBoxUtil.GetMessageBoxStandardWindow(new()
                {
                    ContentTitle = "Warning!!",
                    ContentMessage =
                        $"The mod '{mod.Name}' is already present in the {destination} folder. Do you want to continue? " +
                        $"Continuing with this action will delete the existing mod in the {destination} folder.",
                    ButtonDefinitions = ButtonEnum.YesNo,
                    Icon = Icon.Warning,
                }).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());

                if (res.HasFlag(ButtonResult.No)) shouldQuit = true;
            });
            return shouldQuit;
        }

        public async Task InstallVanilla()
        {
            await _semaphore.WaitAsync();

            try
            {
                await CheckAPI();
                if (_installed.HasVanilla)
                    return;
                
                string managed = _config.ManagedFolder;

                var url = await ModDatabase.FetchVanillaAssemblyLink(_settings);

                (ArraySegment<byte> data, _) = await DownloadFile(url, _ => { });
                
                await _fs.File.WriteAllBytesAsync(Path.Combine(managed, Vanilla), data.Array!);

                await CheckAPI();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <remarks> This enables the API if it's installed! </remarks>
        public async Task InstallApi()
        {
            await _semaphore.WaitAsync();

            try
            {
                await CheckAPI();
                if (_installed.ApiInstall is InstalledState { Enabled: false })
                {
                    // Don't have the toggle update it for us, as that'll infinitely loop.
                    await _ToggleApi(Update.LeaveUnchanged);
                }

                await _InstallApi(_db.Api);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task _InstallApi((string Url, int Version, string SHA256) manifest)
        {
            bool was_vanilla = true;
            if (await CheckAPI())
            {
                if (((InstalledState)_installed.ApiInstall).Version.Major >= manifest.Version)
                    return;

                was_vanilla = false;
            }
            
            (string api_url, int ver, string hash) = manifest;

            string managed = _config.ManagedFolder;

            (ArraySegment<byte> data, string _) = await DownloadFile(api_url, _ => { });
            
            ThrowIfInvalidHash("the API", data, hash);

            // Backup the vanilla assembly
            if (was_vanilla)
                _fs.File.Copy(Path.Combine(managed, Current), Path.Combine(managed, Vanilla), true);

            ExtractZip(data, managed);

            await _installed.RecordApiState(new InstalledState(true, new Version(ver, 0, 0), true));
        }

        public async Task ToggleApi()
        {
            await _semaphore.WaitAsync();

            try
            {
                await _ToggleApi();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task _ToggleApi(Update update = Update.ForceUpdate)
        {
            if (!await CheckAPI()) return;
            
            string managed = _config.ManagedFolder;

            Contract.Assert(_installed.ApiInstall is InstalledState);

            var st = (InstalledState) _installed.ApiInstall;
            
            if (st.Enabled && !_installed.HasVanilla) return;

            var (move_to, move_from) = st.Enabled
                // If the api is enabled, move the current (modded) dll
                // to .m and then take from .v
                ? (Modded, Vanilla)
                // Otherwise, we're enabling the api, so move the current (vanilla) dll
                // And take from our .m file
                : (Vanilla, Modded);
            
            _fs.File.Move(Path.Combine(managed, Current), Path.Combine(managed, move_to), true);
            _fs.File.Move(Path.Combine(managed, move_from), Path.Combine(managed, Current), true);

            await _installed.RecordApiState(st with { Enabled = !st.Enabled });

            // If we're out of date, and re-enabling the api - update it.
            // Note we do this *after* we put the API in place.
            if (update == Update.ForceUpdate && !st.Enabled && st.Version.Major < _db.Api.Version)
                await _InstallApi(_db.Api);
        }

        /// <summary>
        /// Installs the given mod.
        /// </summary>
        /// <param name="mod">Mod to install</param>
        /// <param name="setProgress">Action called to indicate progress asynchronously</param>
        /// <param name="enable">Whether the mod is enabled after installation</param>
        /// <param name="clearDlls">Whether or not to clear all dlls from folder before placing mod</param>
        /// <exception cref="HashMismatchException">Thrown if the download doesn't match the given hash</exception>
        public async Task Install(ModItem mod, Action<ModProgressArgs> setProgress, bool enable, bool clearDlls = false)
        {
            await InstallApi();

            await _semaphore.WaitAsync();

            try
            {
                CreateNeededDirectories();

                void DownloadProgressed(DownloadProgressArgs args)
                {
                    setProgress(new ModProgressArgs {
                        Download = args
                    });
                }

                // Start our progress
                setProgress(new ModProgressArgs());

                await _Install(mod, DownloadProgressed, enable, clearDlls);
                
                setProgress(new ModProgressArgs {
                    Completed = true
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task Uninstall(ModItem mod)
        {
            await _semaphore.WaitAsync();

            try
            {
                // Shouldn't ever not exist, but rather safe than sorry I guess.
                CreateNeededDirectories();

                await _Uninstall(mod);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task _Install(ModItem mod, Action<DownloadProgressArgs> setProgress, bool enable, bool clearDlls = false)
        {
            foreach (ModItem dep in mod.Dependencies.Select(x => _db.Items.First(i => i.Name == x)))
            {
                if (dep.State is InstalledState { Updated: true, Enabled: var enabled })
                {
                    if (!enabled)
                        await Toggle(dep);
                    
                    continue;
                }
                
                if (dep.State is NotInModLinksState)
                {
                    continue; // if its in not in modlinks state, its user's job to update
                }

                // Enable the dependencies' dependencies if we're enabling this mod
                // Or if the dependency was previously not installed.
                await _Install(dep, _ => { }, enable || dep.State is NotInstalledState);
            }

            var cachedModData = await TryGetCachedMod(mod);

            string filename;
            ArraySegment<byte> data;

            if (cachedModData is null)
            {
                (data, filename) = await DownloadFile(mod.Link, setProgress);
            }
            else
            {
                (data, filename) = cachedModData.Value;
            }

            if (!string.IsNullOrEmpty(mod.Sha256) && mod.State is not NotInModLinksState)
                ThrowIfInvalidHash(mod.Name, data, mod.Sha256);
            
            if (cachedModData is null && !string.IsNullOrEmpty(mod.Sha256))
                await CacheMod(mod, filename, data);

            if (clearDlls) 
                ClearDllsFromFolder(mod, enable);

            await PlaceMod(mod, enable, filename, data);
            
            mod.State = mod.State switch {
                ExistsModState ins => new InstalledState(
                    Version: mod.Version,
                    Updated:  true,
                    Enabled: enable,
                    Pinned: ins.Pinned
                ),

                NotInstalledState => new InstalledState(enable, mod.Version, true),

                _ => throw new InvalidOperationException(mod.State.GetType().Name)
            };

            await _installed.RecordInstalledState(mod);
        }

        private void ClearDllsFromFolder(ModItem mod, bool enable)
        {
            string base_folder = enable ? _config.ModsFolder : _config.DisabledFolder;
            string mod_folder = Path.Combine(base_folder, mod.Name);
            
            if (!Directory.Exists(mod_folder)) 
                return;
            
            foreach (var dll_file in Directory.EnumerateFiles(mod_folder).Where(f => f.EndsWith(".dll")))
            {
                try
                {
                    File.Delete(dll_file);
                }
                catch
                {
                    // ignored
                }
            }
        }

        private async Task CacheMod(ModItem mod, string filename, ArraySegment<byte> data)
        {
            try
            {
                if (_config.LowStorageMode) return;

                FileUtil.CreateDirectory(_config.CacheFolder);

                FileUtil.DeleteDirectory(Path.Combine(_config.CacheFolder, mod.Name));

                _fs.Directory.CreateDirectory(Path.Combine(_config.CacheFolder, mod.Name));

                // we save the file name to preserve the dll name if raw dll is given
                await _fs.File.WriteAllTextAsync(Path.Combine(_config.CacheFolder, mod.Name, "filename.txt"), filename);
                await _fs.File.WriteAllBytesAsync(Path.Combine(_config.CacheFolder, mod.Name, filename), data.Array!);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }
        
        /// <summary>
        /// Get a cached mod, returns the data and file name if it is valid
        /// </summary>
        private async Task<(ArraySegment<byte> data, string filename)?> TryGetCachedMod(ModItem mod)
        {
            try
            {
                if (string.IsNullOrEmpty(mod.Sha256)) return null;
                
                if (!_fs.Directory.Exists(_config.CacheFolder)) return null;

                if (!_fs.Directory.Exists(Path.Combine(_config.CacheFolder, mod.Name))) return null;
                if (!_fs.File.Exists(Path.Combine(_config.CacheFolder, mod.Name, "filename.txt"))) return null;

                var filename = await _fs.File.ReadAllTextAsync(Path.Combine(_config.CacheFolder, mod.Name, "filename.txt"));
                var data = await _fs.File.ReadAllBytesAsync(Path.Combine(_config.CacheFolder, mod.Name, filename));

                try
                {
                    ThrowIfInvalidHash(mod.Name, data, mod.Sha256);
                }
                catch
                {
                    return null;
                }

                return (data, filename);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                return null;
            }
        }

        public async Task PlaceMod(ModItem mod, bool enable, string filename, ArraySegment<byte> data)
        {
            // Sometimes our filename is quoted, remove those.
            filename = filename.Trim('"');

            string ext = Path.GetExtension(filename.ToLower());

            // Default to enabling
            string base_folder = enable
                ? _config.ModsFolder
                : _config.DisabledFolder;

            string mod_folder = Path.Combine(base_folder, mod.Name);

            switch (ext)
            {
                case ".zip":
                {
                    ExtractZip(data, mod_folder);

                    break;
                }

                case ".dll":
                {
                    Directory.CreateDirectory(mod_folder);

                    await _fs.File.WriteAllBytesAsync(Path.Combine(mod_folder, filename), data.Array!);

                    break;
                }

                default:
                {
                    throw new NotImplementedException($"Unknown file type for mod download: {filename}");
                }
            }
        }

        private static void ThrowIfInvalidHash(string name, ArraySegment<byte> data, string modSha256)
        {
            var sha = SHA256.Create();

            byte[] hash = sha.ComputeHash(data.AsMemory().AsStream());

            string strHash = BitConverter.ToString(hash).Replace("-", string.Empty);

            if (!string.Equals(strHash, modSha256, StringComparison.OrdinalIgnoreCase))
                throw new HashMismatchException(name, actual: strHash, expected: modSha256);
        }

        private async Task<(ArraySegment<byte> data, string filename)> DownloadFile(
            string uri, Action<DownloadProgressArgs> setProgress)
        {
            (ArraySegment<byte> bytes, HttpResponseMessage response) = await _hc.DownloadBytesWithProgressAsync(
                _settings,
                new Uri(uri), 
                new Progress<DownloadProgressArgs>(setProgress)
            );

            string? filename = string.Empty;

            if (response.Content.Headers.ContentDisposition is { } disposition)
                filename = disposition.FileName;

            if (string.IsNullOrEmpty(filename))
                filename = uri[(uri.LastIndexOf("/", StringComparison.Ordinal) + 1)..];

            return (bytes, filename);
        }

        private void ExtractZip(ArraySegment<byte> data, string root)
        {
            using var archive = new ZipArchive(data.AsMemory().AsStream());

            string dest_dir_path = CreateDirectoryPath(root);

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string file_dest = Path.GetFullPath(Path.Combine(dest_dir_path, entry.FullName));

                if (!file_dest.StartsWith(dest_dir_path))
                    throw new IOException("Extracts outside of directory!");

                // If it's a directory:
                if (Path.GetFileName(file_dest).Length == 0)
                {
                    _fs.Directory.CreateDirectory(file_dest);
                }
                // File
                else
                {
                    // Create containing directory:
                    _fs.Directory.CreateDirectory(Path.GetDirectoryName(file_dest)!);

                    ExtractToFile(entry, file_dest);
                }
            }
        }

        private void ExtractToFile(ZipArchiveEntry src, string dest)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dest == null)
                throw new ArgumentNullException(nameof(dest));

            // Rely on FileStream's ctor for further checking dest parameter
            const FileMode fMode = FileMode.Create;

            using (Stream fs = _fs.FileStream.New(dest, fMode, FileAccess.Write, FileShare.None, 0x1000, false))
            {
                using (Stream es = src.Open())
                    es.CopyTo(fs);
            }

            _fs.File.SetLastWriteTime(dest, src.LastWriteTime.DateTime);
        }


        private string CreateDirectoryPath(string path)
        {
            // Note that this will give us a good DirectoryInfo even if destinationDirectoryName exists:
            IDirectoryInfo di = _fs.Directory.CreateDirectory(path);

            string dest_dir_path = di.FullName;

            if (!dest_dir_path.EndsWith(Path.DirectorySeparatorChar))
                dest_dir_path += Path.DirectorySeparatorChar;

            return dest_dir_path;
        }

        private async Task _Uninstall(ModItem mod)
        {
            var enabled = ((ExistsModState)mod.State).Enabled;
            string dir = Path.Combine
            (
                enabled
                    ? _config.ModsFolder
                    : _config.DisabledFolder,
                mod.Name
            );

            try
            {
                _fs.Directory.Delete(dir, true);
            }
            catch (DirectoryNotFoundException)
            {
                /* oh well, it's uninstalled anyways */
            }

            if (mod.State is NotInModLinksState {ModlinksMod: false} notInModLinksState)
            {
                mod.State = notInModLinksState with { Installed = false };
                _db.Items.Remove(mod);
            }
            else
            {
                mod.State = new NotInstalledState();
            }
            await _installed.RecordUninstall(mod);
        }
        
        public async Task<bool> CheckAPI()
        {
            _installed.HasVanilla =
                _checkValidityOfAssembly.CheckVanillaFileValidity(Vanilla);
            
            int? current_version = _checkValidityOfAssembly.GetAPIVersion(Current);
            bool enabled = true;
            if(current_version == null)
            {
                enabled = false;
                current_version = _checkValidityOfAssembly.GetAPIVersion(Modded);
            }
            
            if (current_version == null)
            {
                await _installed.RecordApiState(new NotInstalledState());
                return false;
            }
            
            if (_installed.ApiInstall is not InstalledState api_state)
            {
                await _installed.RecordApiState(new InstalledState(enabled, new((int)current_version, 0, 0), false));
                return true;
            }
            
            if (api_state.Version.Major != current_version || api_state.Enabled != enabled)
            {
                await _installed.RecordApiState(new InstalledState(enabled, new((int)current_version, 0, 0), api_state.Updated));
            }
            
            return true;
        }
    }
}