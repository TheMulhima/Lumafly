using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Avalonia;
using Lumafly.Interfaces;
using Lumafly.Models;
using Lumafly.Util;

namespace Lumafly.Services
{
    public class ModDatabase : IModDatabase
    {
        // Just for test
        public const string LINKS_BASE_MAP = "https://raw.githubusercontent.com/TheMulhima/Lumafly/static-resources/ModLinks.json";

        private const string VanillaApiRepo = "https://raw.githubusercontent.com/TheMulhima/Lumafly/static-resources/AssemblyLinks.json";


        private static Dictionary<string, string>? modlinks_base_map = null;

        private static async Task FetchModLinksBaseMap(HttpClient hc, ISettings? settings)
        {
            if(modlinks_base_map != null)
            {
                return;
            }

            var json = JsonDocument.Parse(await Fetch(hc, settings, new(LINKS_BASE_MAP)));
            modlinks_base_map = json.Deserialize<Dictionary<string, string>>();

            Debug.Assert(modlinks_base_map != null);
            Debug.Assert(modlinks_base_map["default"] != null);
        }

        private static string GetLinksBase(ISettings settings, ICheckValidityOfAssembly checkValidityOfAssembly)
        {
            Debug.Assert(modlinks_base_map != null);

            if (settings.GameVersion == null)
            {
                checkValidityOfAssembly.GetAPIVersion(Installer.Current, out var gameVersionString);

                if (!Version.TryParse(gameVersionString, out var gameVersion))
                {
                    throw new InvalidOperationException("Invalid game file");
                }

                settings.GameVersion = gameVersion;
            }

            settings.IsOldMode = false;

            var verString = settings.GameVersion.ToString();

            if (!modlinks_base_map.TryGetValue(verString, out var linksBase))
            {
                linksBase = modlinks_base_map["default"];
            }

           if(settings.GameVersion == new Version("1.4.3.2"))
            {
                // 1432 modding api
                settings.IsOldMode = true;
            }

            return linksBase;
        }

        public static string GetModlinksUri(ISettings settings, ICheckValidityOfAssembly checkValidityOfAssembly)
        {
            return GetLinksBase(settings, checkValidityOfAssembly) + "/ModLinks.xml";
        }

        private static string GetAPILinksUri(ISettings settings, ICheckValidityOfAssembly checkValidityOfAssembly)
        {
            return GetLinksBase(settings, checkValidityOfAssembly) + "/ApiLinks.xml";
        }

        internal const int TIMEOUT = 30_000;

        public (string Url, int Version, string SHA256) Api { get; }

        public List<ModItem> Items => _items;

        private readonly List<ModItem> _items = [];
        private readonly List<string> _itemNames = [];

        private ModDatabase(IModSource mods, 
            IGlobalSettingsFinder _settingsFinder, 
            ModLinks ml, 
            ApiLinks al, 
            ISettings? settings = null)
        {
            foreach (var mod in ml.Manifests)
            {
                var oslink = mod.Links.GetOSLink(settings);
                var item = new ModItem
                (
                    settings,
                    link: oslink.URL,
                    version: mod.Version.Value,
                    name: mod.Name,
                    shasum: oslink.SHA256,
                    description: mod.Description,
                    repository: mod.Repository,
                    issues: mod.Issues,
                    rawReadMeURL: mod.ReadMe,
                    isOldStyleMod: settings?.IsOldMode ?? false,
                    dependencies: mod.Dependencies,
                    
                    tags: mod.Tags,
                    integrations: mod.Integrations,
                    authors: mod.Authors,
                    
                    state: mods.FromManifest(mod)
                    
                );
                
                _items.Add(item);
                _itemNames.Add(mod.Name);
            }

            if (settings is not null && !settings.IsOldMode)
            {
                foreach (var (externalModName, externalModState) in mods.NotInModlinksMods)
                {
                    if (externalModState.ModlinksMod)
                    {
                        var mod = _items.First(x => x.Name == externalModName);
                        mod.State = externalModState;
                    }
                    else
                    {
                        _items.Add(ModItem.Empty(
                            settings,
                            state: externalModState,
                            name: externalModName,
                            description: "This mod is not from official modlinks"));
                    }
                }
            }

            _items.Sort((a, b) => string.Compare(a.Name, b.Name));
            _items.ForEach(i => i.FindSettingsFile(_settingsFinder));

            var apiOSLink = al.Manifest.Links.GetOSLink(settings);
            Api = (apiOSLink.URL, al.Manifest.Version, apiOSLink.SHA256);
        }

        public ModDatabase(IModSource mods, IGlobalSettingsFinder settingsFinder, (ModLinks ml, ApiLinks al) links, ISettings settings) 
            : this(mods, settingsFinder, links.ml, links.al, settings) { }

        public ModDatabase(IModSource mods, IGlobalSettingsFinder settingsFinder, string modlinks, string apilinks) 
            : this(mods, settingsFinder, FromString<ModLinks>(modlinks), FromString<ApiLinks>(apilinks)) { }
        
        public static async Task<(ModLinks, ApiLinks)> FetchContent(HttpClient hc, 
            ISettings settings, ICheckValidityOfAssembly checkValidityOfAssembly,
            bool fetchOfficial = true)
        {

            // although slower to fetch one by one, prevents silent errors and hence resulting in
            // empty screen with no error
            ModLinks ml = await FetchModLinks(hc, settings, checkValidityOfAssembly, fetchOfficial);
            ApiLinks al = await FetchApiLinks(hc, settings, checkValidityOfAssembly);

            return (ml, al);
        }

        public static T FromString<T>(string xml) where T : XmlDataContainer
        {
            var serializer = new XmlSerializer(typeof(T));
            
            using TextReader reader = new StringReader(xml);

            var obj = (T?) serializer.Deserialize(reader);

            if (obj is null)
                throw new InvalidDataException();

            obj.Raw = xml;

            return obj;
        }

        private static async Task<ApiLinks> FetchApiLinks(HttpClient hc, ISettings settings, ICheckValidityOfAssembly checkValidityOfAssembly)
        {
            await FetchModLinksBaseMap(hc, settings);
            return FromString<ApiLinks>(await Fetch(hc, settings, new Uri(GetAPILinksUri(settings, checkValidityOfAssembly))));
        }
        
        private static async Task<ModLinks> FetchModLinks(HttpClient hc, ISettings settings, ICheckValidityOfAssembly checkValidityOfAssembly, bool fetchOfficial)
        {
            if (!fetchOfficial && settings.UseCustomModlinks)
            {
                try
                {
                    var modlinksUri = new Uri(settings.CustomModlinksUri);
                    if (modlinksUri.IsFile)
                    {
                        return FromString<ModLinks>(await File.ReadAllTextAsync(modlinksUri.LocalPath));
                    }

                    var cts = new CancellationTokenSource(TIMEOUT);

                    //get raw versions of common urls
                    Regex githubRegex = new Regex(@"^(http(s?):\/\/)?(www\.)?github.com?");
                    Regex pasteBinRegex = new Regex(@"^(http(s?):\/\/)?(www\.)?pastebin.com?");

                    if (githubRegex.IsMatch(settings.CustomModlinksUri))
                    {
                        settings.CustomModlinksUri = settings.CustomModlinksUri
                            .Replace("github.com", "raw.githubusercontent.com").Replace("/blob/", "/");
                    }
                    if (pasteBinRegex.IsMatch(settings.CustomModlinksUri))
                    {
                        settings.CustomModlinksUri = settings.CustomModlinksUri.Replace("pastebin.com", "pastebin.com/raw");
                    }
                    
                    return FromString<ModLinks>(await hc.GetStringAsync2(settings, new Uri(settings.CustomModlinksUri), cts.Token));
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Unable to load custom modlinks because {e}");
                    throw new InvalidModlinksException();
                }
            }

            await FetchModLinksBaseMap(hc, settings);
            return FromString<ModLinks>(await Fetch(hc, settings, new Uri(GetModlinksUri(settings, checkValidityOfAssembly))));
            
        }

        private static async Task<string> Fetch(HttpClient hc, ISettings? settings, Uri uri)
        {
            using var cts = new CancellationTokenSource(TIMEOUT);
            return await hc.GetStringAsync2(settings, uri, cts.Token);
        }

        public static async Task<string> FetchVanillaAssemblyLink(ISettings settings)
        {
            using var cts = new CancellationTokenSource(TIMEOUT);
            var hc = new HttpClient();
            hc.DefaultRequestHeaders.Add("User-Agent", "Lumafly");
            var json = JsonDocument.Parse(await hc.GetStringAsync2(settings, VanillaApiRepo, cts.Token));
            
            var platform = "Windows";
            if(OperatingSystem.IsMacOS())
            {
                platform = "Mac";
            }
            else if(OperatingSystem.IsLinux() && !settings.IsWindowsOrWine)
            {
                platform = "Linux";
            }

            var gameVersion = settings.GameVersion;

            Debug.Assert(gameVersion != null);

            if (json.RootElement.TryGetProperty("by_version", out var byVersion))
            {
                foreach (var version in byVersion.EnumerateObject())
                {
                    if (GameVersion.Normalize(version.Name) == gameVersion
                        && version.Value.TryGetProperty(platform, out var linkElem)
                        && linkElem.GetString() is { } link)
                        return link;
                }
            }

            throw new ReadableError(
                $"Lumafly has no vanilla assembly for Hollow Knight {gameVersion}. " +
                "Please verify integrity of game files instead.");
        }
    }

    public class InvalidModlinksException : Exception
    {
        public InvalidModlinksException() { } 
    }
}