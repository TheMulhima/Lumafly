using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Lumafly.Interfaces;
using Lumafly.Models;
using Lumafly.Util;

namespace Lumafly.Services
{
    [Serializable]
    public record InstalledMods : IModSource
    {
        internal const string FILE_NAME = "InstalledMods.json";
        
        internal static readonly string ConfigPath = Path.Combine(Settings.GetOrCreateDirPath(), FILE_NAME);

        public Dictionary<string, InstalledState> Mods { get; set; } = new();
        public Dictionary<string, NotInModLinksState> NotInModlinksMods { get; set; } = new();
        
        private static readonly SemaphoreSlim _semaphore = new (1);
        
        public bool HasVanilla { get; set; }

        public ModState ApiInstall
        {
            get => (ModState?) _ApiState ?? new NotInstalledState();
            private set => _ApiState = value is InstalledState s ? s : null; 
        } 

        [JsonInclude]
        // public get because System.Text.Json won't let me make both private
        public InstalledState? _ApiState { get; private set; }

        private readonly IFileSystem _fs;
        internal static bool ModExists(ISettings config, string name, out bool enabled)
        {
            enabled = false;
                
            if (Directory.Exists(Path.Combine(config.ModsFolder, name)))
                return enabled = true;

            return Directory.Exists(Path.Combine(config.DisabledFolder, name));
        }
        
        public static async Task<InstalledMods> Load(IFileSystem fs, ISettings config, ModLinks ml)
        {
            InstalledMods db;

            try
            {
                db = JsonSerializer.Deserialize<InstalledMods>(await File.ReadAllTextAsync(ConfigPath))
                    ?? throw new InvalidDataException();
            } catch (Exception e) when (e is InvalidDataException or JsonException or FileNotFoundException)
            {
                // If we have malformed JSON or it's a new install, try and recover any installed mods
                db = new InstalledMods();

                foreach (string name in ml.Manifests.Select(x => x.Name))
                {
                    if (!ModExists(config, name, out bool enabled)) 
                        continue;
                    
                    // Pretend it's out of date because we aren't sure of the version.
                    db.Mods.Add(name, new InstalledState(enabled, new Version(0, 0), false, false));
                }
            }

            // Validate that mods are installed in case of manual user intervention
            foreach (string name in db.Mods.Select(x => x.Key))
            {
                if (ModExists(config, name, out var enabled))
                {
                    if (db.Mods[name].Enabled != enabled)
                    {
                        Trace.WriteLine($"mod {name} enabled state mismatch, fixing!");
                        db.Mods[name] = db.Mods[name] with { Enabled = enabled };
                    }
                    continue;   
                }

                Trace.TraceWarning($"Removing missing mod {name}!");
                
                db.Mods.Remove(name);
            }
            
            // Validate that mods are installed in case of manual user intervention
            foreach (string name in db.NotInModlinksMods.Select(x => x.Key))
            {
                if (ModExists(config, name, out var enabled))
                {
                    if (db.NotInModlinksMods[name].Enabled != enabled)
                    {
                        Trace.WriteLine($"mod {name} enabled state mismatch, fixing!");
                        db.NotInModlinksMods[name] = db.NotInModlinksMods[name] with { Enabled = enabled };
                    }
                    continue;   
                }

                Trace.TraceWarning($"Removing missing mod {name}!");
                
                db.NotInModlinksMods.Remove(name);
            }
            
            // just in case
            FileUtil.CreateDirectory(config.ModsFolder);
            FileUtil.CreateDirectory(config.DisabledFolder);
            
            var currentList = Directory.EnumerateDirectories(config.ModsFolder)
                .Concat(Directory.EnumerateDirectories(config.DisabledFolder));

            // add not in modlinks mods
            foreach (var modNamePath in currentList)
            {
                var modName = Path.GetFileName(modNamePath);
                if (modName == "Disabled") continue; // skip disabled Folder
                
                if (db.Mods.ContainsKey(modName)) continue; // skip if already registered
                if (db.NotInModlinksMods.ContainsKey(modName)) continue; // skip if already registered
                
                var correspondingMod = ml.Manifests.FirstOrDefault(mod => mod.Name == modName);
                
                db.NotInModlinksMods[modName] = new NotInModLinksState(
                    ModlinksMod: correspondingMod != null,
                    Enabled: new DirectoryInfo(modNamePath).Parent?.Name == "Mods");
            }
            
            /*
             * If the user deleted their assembly, we can deal with it at least.
             * 
             * This isn't ideal, but at least we won't crash and the user will be
             * (relatively) okay, and as a budget remedy we'll just put the API in
             */
            if (db.ApiInstall is InstalledState && 
                !fs.File.Exists(Path.Combine(config.ManagedFolder, Installer.Modded)) &&
                !fs.File.Exists(Path.Combine(config.ManagedFolder, Installer.Current))
            ) 
            {
                Trace.TraceWarning("Assembly missing, marking API as uninstalled!");
                db.ApiInstall = new NotInstalledState();
            }

            await db.SaveToDiskAsync();
            
            return db;
        }

        public InstalledMods() => _fs = new FileSystem();

        public InstalledMods(IFileSystem fs) => _fs = fs;

        public async Task Reset()
        {
            Mods.Clear();
            NotInModlinksMods.Clear();
            _ApiState = null;

            await SaveToDiskAsync();
        }
        
        /// <summary>
        /// Sets the installed mods lists to a new list and saves it to disk
        /// </summary>
        /// <param name="mods">The modlinks mods that are installed</param>
        /// <param name="notInModlinksMods">The not in modlinks mods that are installed</param>
        public async Task SetMods(
            Dictionary<string, InstalledState> mods, 
            Dictionary<string, NotInModLinksState> notInModlinksMods)
        {
            Mods = mods;
            NotInModlinksMods = notInModlinksMods;

            await SaveToDiskAsync();
        }

        public async Task RecordApiState(ModState st)
        {
            ApiInstall = st;
            
            await SaveToDiskAsync();
        }

        public ModState FromManifest(Manifest manifest)
        {
            if (Mods.TryGetValue(manifest.Name, out var existingInstalled))
            {
                return existingInstalled with
                {
                    Updated = existingInstalled.Version == manifest.Version.Value
                };
            }
            
            if (NotInModlinksMods.TryGetValue(manifest.Name, out var existingNotInModlinks))
            {
                return existingNotInModlinks;
            }

            return new NotInstalledState();
        }
        
        public async Task RecordInstalledState(ModItem item)
        {
            Contract.Assert(item.State is ExistsModState);

            if (item.State is InstalledState state)
            {
                Mods[item.Name] = state;
                NotInModlinksMods.Remove(item.Name);
            }
            if (item.State is NotInModLinksState notInModLinksState)
            {
                NotInModlinksMods[item.Name] = notInModLinksState;
                Mods.Remove(item.Name);
            }

            await SaveToDiskAsync();
        }

        public async Task RecordUninstall(ModItem item)
        {
            Contract.Assert(item.State is NotInstalledState or NotInModLinksState);

            Mods.Remove(item.Name);
            NotInModlinksMods.Remove(item.Name);

            await SaveToDiskAsync();
        }

        private async Task SaveToDiskAsync()
        {
            await _semaphore.WaitAsync();

            try
            {
                // this probably only happens on reset so best to just yeet the file so all mods
                // dont get categorized as not in modlinks
                if (!Mods.Any() && !NotInModlinksMods.Any() && _ApiState == null)
                {
                    if (_fs.File.Exists(ConfigPath))
                    {
                        _fs.File.Delete(ConfigPath);
                    }
                }
                else
                {
                    await using Stream fs = _fs.File.Exists(ConfigPath)
                        ? _fs.FileStream.New(ConfigPath, FileMode.Truncate)
                        : _fs.File.Create(ConfigPath);


                    await JsonSerializer.SerializeAsync(fs, this, new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    });
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public InstalledMods DeepCopy() => new ()
        {
            _ApiState = _ApiState,
            ApiInstall = ApiInstall with { },
            Mods = Mods.ToDictionary(x => x.Key, x => x.Value),
            NotInModlinksMods = NotInModlinksMods.ToDictionary(x => x.Key, x => x.Value),
            HasVanilla = HasVanilla
        };
    }
}