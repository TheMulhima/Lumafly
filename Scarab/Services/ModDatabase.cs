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
        private const string MODLINKS_BASE_URI = "https://raw.githubusercontent.com/hk-modding/modlinks/";
        private const string APILINKS_BASE_URI = "https://raw.githubusercontent.com/hk-modding/modlinks/";
        
        private const string FALLBACK_MODLINKS_URI = "https://cdn.jsdelivr.net/gh/hk-modding/modlinks@latest/ModLinks.xml";
        private const string FALLBACK_APILINKS_URI = "https://cdn.jsdelivr.net/gh/hk-modding/modlinks@latest/ApiLinks.xml";
        
        private const string VanillaApiRepo = "https://raw.githubusercontent.com/TheMulhima/Scarab/static-resources/AssemblyLinks.json";
        
        public static string GetModlinksUri(string? sha = null) => MODLINKS_BASE_URI + (sha ?? "main") + "/ModLinks.xml";
        private static string GetAPILinksUri() => APILINKS_BASE_URI + "main/ApiLinks.xml";

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

                    mod.State = new NotInModLinksState(Enabled: enabled, ModlinksMod:true);
                }
                else
                {
                    _items.Add(ModItem.Empty(
                        state: new NotInModLinksState(Enabled: enabled, ModlinksMod:false),
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
        
        public static async Task<(ModLinks, ApiLinks)> FetchContent(HttpClient hc, ISettings _settings, bool fetchOfficial = true)
        {
            // although slower to fetch one by one, prevents silent errors and hence resulting in 
            // empty screen with no error
            ModLinks ml = await FetchModLinks(hc, _settings, fetchOfficial);
            ApiLinks al = await FetchApiLinks(hc);

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

        private static async Task<ApiLinks> FetchApiLinks(HttpClient hc)
        {
            return FromString<ApiLinks>(await FetchWithFallback(hc, new Uri(GetAPILinksUri()), new Uri(FALLBACK_APILINKS_URI)));
        }
        
        private static async Task<ModLinks> FetchModLinks(HttpClient hc, ISettings _settings, bool fetchOfficial)
        {
            if (!fetchOfficial && _settings.UseCustomModlinks)
            {
                try
                {
                    var modlinksUri = new Uri(_settings.CustomModlinksUri);
                    if (modlinksUri.IsFile)
                    {
                        return FromString<ModLinks>(await File.ReadAllTextAsync(modlinksUri.LocalPath));
                    }

                    var cts = new CancellationTokenSource(TIMEOUT);

                    //get raw versions of common urls
                    Regex githubRegex = new Regex(@"^(http(s?):\/\/)?(www\.)?github.com?");
                    Regex pasteBinRegex = new Regex(@"^(http(s?):\/\/)?(www\.)?pastebin.com?");

                    if (githubRegex.IsMatch(_settings.CustomModlinksUri))
                    {
                        _settings.CustomModlinksUri = _settings.CustomModlinksUri
                            .Replace("github.com", "raw.githubusercontent.com").Replace("/blob/", "/");
                    }
                    if (pasteBinRegex.IsMatch(_settings.CustomModlinksUri))
                    {
                        _settings.CustomModlinksUri = _settings.CustomModlinksUri.Replace("pastebin.com", "pastebin.com/raw");
                    }
                    
                    return FromString<ModLinks>(await hc.GetStringAsync(new Uri(_settings.CustomModlinksUri), cts.Token));
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Unable to load custom modlinks because {e}");
                    throw new InvalidModlinksException();
                }
            }

            return FromString<ModLinks>(await FetchWithFallback(hc, new Uri(GetModlinksUri()), new Uri(FALLBACK_MODLINKS_URI)));
            
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