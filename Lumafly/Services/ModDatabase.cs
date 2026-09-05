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
        public const string LINKS_LATEST_BASE = "https://raw.githubusercontent.com/hk-modding/modlinks/main";
        // TODO: there is a thing like tags, or branches for when the change happens
        public const string LINKS_1578_BASE = "https://raw.githubusercontent.com/hk-modding/modlinks/6f68dbcce825b6b0e5464e36fd5ad10fc9ba72fb";

        public const string LINKS_1432_BASE = "https://raw.githubusercontent.com/FrostyTwilight/modlinks-1432/refs/heads/master";

        private const string VanillaApiRepo = "https://raw.githubusercontent.com/TheMulhima/Lumafly/static-resources/AssemblyLinks.json";

        private static string GetLinksBase(ISettings settings, ICheckValidityOfAssembly checkValidityOfAssembly)
        {
            if (settings.GameVersion == null)
            {
                checkValidityOfAssembly.GetAPIVersion(Installer.Current, out var gameVersionString);

                if (!Version.TryParse(gameVersionString, out var gameVersion))
                {
                    throw new InvalidOperationException("Invalid game file");
                }

                settings.GameVersion = gameVersion;
            }

            string linksBase;
            settings.IsOldMode = false;
            if (settings.GameVersion >= new Version("1.5.12620"))
            {
                // New modding api (latest)
                linksBase = LINKS_LATEST_BASE;
            }
            else if (settings.GameVersion == new Version("1.5.78.11833"))
            {
                // Old modding api 
                // See https://discord.com/channels/879125729936298015/913460282750291968/1533483165845557349
                linksBase = LINKS_1578_BASE;
            }
            else if(settings.GameVersion == new Version("1.4.3.2"))
            {
                // 1432 modding api
                linksBase = LINKS_1432_BASE;
                settings.IsOldMode = true;
            }
            else
            {
                throw new NotSupportedException($"The current version of the game is not supported. ({settings.GameVersion})");
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

        private readonly List<ModItem> _items = new();
        private readonly List<string> _itemNames = new();

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

            return FromString<ModLinks>(await Fetch(hc, settings, new Uri(GetModlinksUri(settings, checkValidityOfAssembly))));
            
        }

        private static async Task<string> Fetch(HttpClient hc, ISettings? settings, Uri uri)
        {
            using var cts = new CancellationTokenSource(TIMEOUT);
            return await hc.GetStringAsync2(settings, uri, cts.Token);
        }

        public static async Task<string> FetchVanillaAssemblyLink(ISettings? settings)
        {
            using var cts = new CancellationTokenSource(TIMEOUT);
            var hc = new HttpClient();
            hc.DefaultRequestHeaders.Add("User-Agent", "Lumafly");
            var json = JsonDocument.Parse(await hc.GetStringAsync2(settings, VanillaApiRepo, cts.Token));
            
            var jsonKey = "Assembly-CSharp.dll.v";
            // windows assembly is just called that because initially this was overlooked and only windows assembly was downloaded
            if (OperatingSystem.IsMacOS()) jsonKey = "Mac-Assembly-CSharp.dll.v";
            if (OperatingSystem.IsLinux()) jsonKey = "Linux-Assembly-CSharp.dll.v";

            jsonKey = $"{settings?.GameVersion}-${jsonKey}";
            
            json.RootElement.TryGetProperty(jsonKey, out var linkElem);
            
            var link = linkElem.GetString();
            if (link != null)
                return link;
            throw new Exception("Lumafly was unable to get vanilla assembly link from its resources. Please verify integrity of game files instead");
        }
    }

    public class InvalidModlinksException : Exception
    {
        public InvalidModlinksException() { } 
    }
}