using Microsoft.Win32;
using Lumafly.Interfaces;
using Lumafly.Models;
using Lumafly.Util;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lumafly.Enums;
using Lumafly.Services;
using System.Threading;
using System.Threading.Tasks;

namespace Lumafly
{
    [Serializable]
    public class Settings : ISettings
    {
        public string ManagedFolder { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AutoRemoveUnusedDepsOptions AutoRemoveUnusedDeps { get; set; } = AutoRemoveUnusedDepsOptions.Never;
        public bool WarnBeforeRemovingDependents { get; set; } = true;
        public bool UseCustomModlinks { get; set; }
        public string CustomModlinksUri { get; set; } = string.Empty;
        public bool UseGithubMirror { get; set; }
        public string GithubMirrorFormat { get; set; } = string.Empty;
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SupportedLanguages? PreferredLanguage { get; set; }
        public bool LowStorageMode { get; set; } = false;
        public string ExtraSpaceTaken
        {
            get
            {
                long size = 0;
                if (Directory.Exists(CacheFolder))
                {
                    size += FileUtil.GetAllFilesInDirectory(CacheFolder).Sum(x => x.Length);
                }

                var managed = new DirectoryInfo(ManagedFolder);
                foreach (var dir in managed.EnumerateDirectories())
                {
                    if (dir.GetFiles().Any(x => x.Name == PackManager.packInfoFileName))
                    {
                        size += FileUtil.GetAllFilesInDirectory(dir.FullName).Sum(x => x.Length);
                    }
                }
                
                return $"{size / 1024 / 1024} MB";
            }
        }

        public bool RequiresWorkaroundClient { get; set; }

        // @formatter:off
        private static readonly ImmutableList<string> STATIC_PATHS = new List<string>
        {
            "Program Files/Steam/steamapps/common/Hollow Knight",
            "XboxGames/Hollow Knight/Content",
            "Program Files (x86)/Steam/steamapps/common/Hollow Knight",
            "Program Files/GOG Galaxy/Games/Hollow Knight",
            "Program Files (x86)/GOG Galaxy/Games/Hollow Knight",
            "Steam/steamapps/common/Hollow Knight",
            "GOG Galaxy/Games/Hollow Knight"
        }
        .SelectMany(path => DriveInfo.GetDrives().Select(d => Path.Combine(d.Name, path))).ToImmutableList();

        private static readonly ImmutableList<string> USER_SUFFIX_PATHS = new List<string>
        {
            // Default locations on linux
            ".local/share/Steam/steamapps/common/Hollow Knight",
            ".steam/steam/steamapps/common/Hollow Knight",
            // Flatpak
            ".var/app/ocm.valvesoftware.Steam/data/Steam/steamapps/common",
            // Symlinks to the Steam root on linux
            ".steam/steam",
            ".steam/root",
            // Default for macOS
            "Library/Application Support/Steam/steamapps/common/Hollow Knight/hollow_knight.app"
        }
        .ToImmutableList();
        // @formatter:on
        
        public static string ConfigFolderPath => Path.Combine
        (
            Environment.GetFolderPath
            (
                Environment.SpecialFolder.ApplicationData,
                Environment.SpecialFolderOption.Create
            ),
            "HKModInstaller"
        );
        
        private static string ConfigPath => Path.Combine(ConfigFolderPath, "HKInstallerSettings.json");
        public string CacheFolder => Path.Combine(ConfigFolderPath, "HKInstallerCache");

        internal Settings(string path)
        {
            ManagedFolder = path;
            
            var culture = Thread.CurrentThread.CurrentUICulture;
            if (Enum.TryParse(culture.TwoLetterISOLanguageName, out SupportedLanguages preferredLanguage))
                PreferredLanguage = preferredLanguage;
        }

        // Used by serializer.
        public Settings()
        {
            ManagedFolder = null!;
            AutoRemoveUnusedDeps = AutoRemoveUnusedDepsOptions.Never;
            PreferredLanguage = null;
            LowStorageMode = false;
        }

        public static string GetOrCreateDirPath()
        {
            string dirPath = Path.GetDirectoryName(ConfigPath) ?? throw new InvalidOperationException();

            // No-op if path already exists.
            Directory.CreateDirectory(dirPath);

            return dirPath;
        }

        internal static async Task<ValidPath?> TryAutoDetect()
        {
            ValidPath? path = null;
            path = STATIC_PATHS.Select(PathUtil.ValidateWithSuffix).FirstOrDefault(x => x is not null);

            // If that's valid, use it.
            if (path is not null)
                return path;

            // Otherwise, we go through the user profile suffixes.
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            path = USER_SUFFIX_PATHS
                   .Select(suffix => Path.Combine(home, suffix))
                   .Select(PathUtil.ValidateWithSuffix)
                   .FirstOrDefault(x => x is not null);

            if (path is not null)
                return path;

            if (TryDetectFromRegistry(out path))
                return path;
            
            // since it cant detect from registry assume its because it can't access the registry
            await DisplayErrors.AskForAdminReload("Path was not automatically found from registry.");

            return path; // if couldn't find path it would be null
        }

        private static bool TryDetectFromRegistry([MaybeNullWhen(false)] out ValidPath path)
        {
            path = null;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            return TryDetectSteamRegistry(out path) || TryDetectGogRegistry(out path);
        }

        [SupportedOSPlatform(nameof(OSPlatform.Windows))]
        private static bool TryDetectGogRegistry([MaybeNullWhen(false)] out ValidPath path)
        {
            path = null;

            if (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\Games\1308320804", "workingDir", null) is not string gog_path)
                return false;

            // Double check, just in case.
            if (PathUtil.ValidateWithSuffix(gog_path) is not ValidPath vpath)
                return false;

            path = vpath;

            return true;
        }

        [SupportedOSPlatform(nameof(OSPlatform.Windows))]
        private static bool TryDetectSteamRegistry([MaybeNullWhen(false)] out ValidPath path)
        {
            path = null;

            if (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) is not string steam_install)
                return false;

            IEnumerable<string> lines;

            try
            {
                lines = File.ReadLines(Path.Combine(steam_install, "steamapps", "libraryfolders.vdf"));
            }
            catch (Exception e) when (
                e is FileNotFoundException
                    or UnauthorizedAccessException
                    or IOException
                    or DirectoryNotFoundException
            )
            {
                return false;
            }

            string? Parse(string line)
            {
                line = line.TrimStart();

                if (!line.StartsWith("\"path\""))
                    return null;

                string[] pair = line.Split("\t", 2, StringSplitOptions.RemoveEmptyEntries);

                return pair.Length != 2
                    ? null
                    : pair[1].Trim('"');
            }

            IEnumerable<string> library_paths = lines.Select(Parse).OfType<string>();

            path = library_paths.Select(library_path => Path.Combine(library_path, "steamapps", "common", "Hollow Knight"))
                                .Select(PathUtil.ValidateWithSuffix)
                                .FirstOrDefault(x => x is not null);

            return path is not null;
        }

        public static Settings? Load()
        {
            if (!File.Exists(ConfigPath))
                return null;

            Debug.WriteLine($"ConfigPath: File @ {ConfigPath} exists.");

            string content = File.ReadAllText(ConfigPath);

            try
            {
                return JsonSerializer.Deserialize<Settings>(content);
            }
            // The JSON is malformed, act as if we don't have settings as a backup
            catch (Exception e) when (e is JsonException or ArgumentNullException)
            {
                return null;
            }
        }

        public static Settings Create(string path)
        {
            // Create from ManagedPath.
            var settings = new Settings(path);

            settings.Save();

            return settings;
        }

        public void Save()
        {
            string content = JsonSerializer.Serialize(this, new JsonSerializerOptions()
            {
                WriteIndented = true,
            });

            GetOrCreateDirPath();

            string path = ConfigPath;

            File.WriteAllText(path, content);
        }
    }
}