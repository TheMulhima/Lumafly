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
using Scarab.Interfaces;
using Scarab.Models;

namespace Scarab.Services
{
    public class ModDatabase : IModDatabase
    {
        public const string DEFAULT_LINKS_BASE = "https://raw.githubusercontent.com/hk-modding/modlinks/main";
        
        private const string FALLBACK_MODLINKS_URI = "https://cdn.jsdelivr.net/gh/hk-modding/modlinks@latest/ModLinks.xml";
        private const string FALLBACK_APILINKS_URI = "https://cdn.jsdelivr.net/gh/hk-modding/modlinks@latest/ApiLinks.xml";
        
        private const string VanillaApiRepo = "https://raw.githubusercontent.com/TheMulhima/Scarab/static-resources/AssemblyLinks.json";
        
        public static string GetModlinksUri(ISettings settings, string? sha = null)
        {
            if (settings.BaseLink == DEFAULT_LINKS_BASE)
            {
                // if we need to get a specific modlinks version we only do it for when base link the official version
                return settings.BaseLink.Replace("main", sha ?? "main") + "/ModLinks.xml";
            }
            else return settings.BaseLink + "/ModLinks.xml";
        }

        private static string GetAPILinksUri(ISettings settings) => settings.BaseLink + "/ApiLinks.xml";

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
                var item = new ModItem
                (
                    link: mod.Links.OSUrl,
                    version: mod.Version.Value,
                    name: mod.Name,
                    shasum: mod.Links.SHA256,
                    description: mod.Description,
                    repository: mod.Repository,
                    dependencies: mod.Dependencies,
                    
                    tags: mod.Tags,
                    integrations: mod.Integrations,
                    authors: mod.Authors,
                    
                    state: mods.FromManifest(mod)
                    
                );
                
                _items.Add(item);
                _itemNames.Add(mod.Name);
            }


            if (settings is not null && Directory.Exists(settings.ModsFolder))
            {
                foreach (var dir in Directory.GetDirectories(settings.ModsFolder).Where(x => x != settings.DisabledFolder))
                {
                    AddExternalMod(dir, true);
                }

                if (Directory.Exists(settings.DisabledFolder))
                {
                    foreach (var dir in Directory.GetDirectories(settings.DisabledFolder))
                    {
                        AddExternalMod(dir, false);
                    }
                }
            }

            void AddExternalMod(string dir, bool enabled)
            {
                // get only folder name
                var name = new DirectoryInfo(dir).Name;

                var isModlinksMod = _itemNames.Contains(name);
                if (isModlinksMod)
                {
                    var mod = _items.First(x => x.Name == name);
                    if (mod.Installed) 
                        return;

                    mod.State = new NotInModLinksState(ModlinksMod:true, Enabled: enabled);
                }
                else
                {
                    _items.Add(ModItem.Empty(
                        state: new NotInModLinksState(ModlinksMod:false, Enabled: enabled),
                        name: name,
                        description: "This mod is not from official modlinks"));
                }
            }

            _items.Sort((a, b) => string.Compare(a.Name, b.Name));
            _items.ForEach(i => i.FindSettingsFile(_settingsFinder));

            Api = (al.Manifest.Links.OSUrl, al.Manifest.Version, al.Manifest.Links.SHA256);
        }

        public ModDatabase(IModSource mods, IGlobalSettingsFinder settingsFinder, (ModLinks ml, ApiLinks al) links, ISettings settings) 
            : this(mods, settingsFinder, links.ml, links.al, settings) { }

        public ModDatabase(IModSource mods, IGlobalSettingsFinder settingsFinder, string modlinks, string apilinks) 
            : this(mods, settingsFinder, FromString<ModLinks>(modlinks), FromString<ApiLinks>(apilinks)) { }
        
        public static async Task<(ModLinks, ApiLinks)> FetchContent(HttpClient hc, ISettings settings, bool fetchOfficial = true)
        {
            // although slower to fetch one by one, prevents silent errors and hence resulting in 
            // empty screen with no error
            ModLinks ml = await FetchModLinks(hc, settings, fetchOfficial);
            ApiLinks al = await FetchApiLinks(hc, settings);

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

        private static async Task<ApiLinks> FetchApiLinks(HttpClient hc, ISettings settings)
        {
            return FromString<ApiLinks>(await FetchWithFallback(hc, new Uri(GetAPILinksUri(settings)), new Uri(FALLBACK_APILINKS_URI)));
        }
        
        private static async Task<ModLinks> FetchModLinks(HttpClient hc, ISettings settings, bool fetchOfficial)
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
                    
                    return FromString<ModLinks>(await hc.GetStringAsync(new Uri(settings.CustomModlinksUri), cts.Token));
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Unable to load custom modlinks because {e}");
                    throw new InvalidModlinksException();
                }
            }

            return FromString<ModLinks>(await FetchWithFallback(hc, new Uri(GetModlinksUri(settings)), new Uri(FALLBACK_MODLINKS_URI)));
            
        }

        private static async Task<string> FetchWithFallback(HttpClient hc, Uri uri, Uri fallback)
        {
            try
            {
                var cts = new CancellationTokenSource(TIMEOUT);
                return await hc.GetStringAsync(uri, cts.Token);
            }
            catch (Exception e) when (e is TaskCanceledException or HttpRequestException)
            {
                var cts = new CancellationTokenSource(TIMEOUT);
                return await hc.GetStringAsync(fallback, cts.Token);
            }
        }

        public static async Task<string> FetchVanillaAssemblyLink()
        {
            var cts = new CancellationTokenSource(TIMEOUT);
            var hc = new HttpClient();
            hc.DefaultRequestHeaders.Add("User-Agent", "Scarab");
            var json = JsonDocument.Parse(await hc.GetStringAsync(VanillaApiRepo, cts.Token));
            json.RootElement.TryGetProperty("Assembly-CSharp.dll.v", out var linkElem);
            
            var link = linkElem.GetString();
            if (link != null)
            {
                return link;
            }
            throw new Exception("Scarab was unable to get vanilla assembly link from its resources. Please verify integrity of game files instead");
        }
    }

    public class InvalidModlinksException : Exception
    {
        public InvalidModlinksException() { } 
    }
}