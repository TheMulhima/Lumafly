using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Scarab.Interfaces;
using Scarab.Models;

namespace Scarab.Services
{
    public class ModDatabase : IModDatabase
    {
        private const string MODLINKS_URI = "https://raw.githubusercontent.com/hk-modding/modlinks/main/ModLinks.xml";
        private const string APILINKS_URI = "https://raw.githubusercontent.com/hk-modding/modlinks/main/ApiLinks.xml";
        
        private const string FALLBACK_MODLINKS_URI = "https://cdn.jsdelivr.net/gh/hk-modding/modlinks@latest/ModLinks.xml";
        private const string FALLBACK_APILINKS_URI = "https://cdn.jsdelivr.net/gh/hk-modding/modlinks@latest/ApiLinks.xml";

        public (string Url, int Version, string SHA256) Api { get; }

        public List<ModItem> Items => _items;

        private readonly List<ModItem> _items = new();
        private readonly List<string> _itemNames = new();

        private ModDatabase(IModSource mods, ModLinks ml, ApiLinks al, ISettings? settings = null)
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
                    AddExternalMod(dir, settings.ModsFolder, true);
                }

                if (Directory.Exists(settings.DisabledFolder))
                {
                    foreach (var dir in Directory.GetDirectories(settings.DisabledFolder))
                    {
                        AddExternalMod(dir, settings.DisabledFolder, false);
                    }
                }
            }

            void AddExternalMod(string dir, string baseDir ,bool enabled)
            {
                // get only folder name
                var name = dir.Replace(baseDir + "\\", "");

                // check if its a modlinks mod and if its installed. if both are true don't add the not in modlinks mod
                if (_itemNames.Contains(name) && _items.First(i => i.Name == name).Installed)
                    return;

                _items.Add(ModItem.Empty(
                    state: new NotInModLinksState(enabled),
                    name: name,
                    description: "This mod is not from official modlinks"));
            }

            _items.Sort((a, b) => string.Compare(a.Name, b.Name));

            Api = (al.Manifest.Links.OSUrl, al.Manifest.Version, al.Manifest.Links.SHA256);
        }

        public ModDatabase(IModSource mods, (ModLinks ml, ApiLinks al) links, ISettings settings) : this(mods, links.ml, links.al, settings) { }

        public ModDatabase(IModSource mods, string modlinks, string apilinks) : this(mods, FromString<ModLinks>(modlinks), FromString<ApiLinks>(apilinks)) { }
        
        public static async Task<(ModLinks, ApiLinks)> FetchContent(HttpClient hc)
        {
            Task<ModLinks> ml = FetchModLinks(hc);
            Task<ApiLinks> al = FetchApiLinks(hc);

            return (await ml, await al);
        }
        
        private static T FromString<T>(string xml)
        {
            var serializer = new XmlSerializer(typeof(T));
            
            using TextReader reader = new StringReader(xml);

            var obj = (T?) serializer.Deserialize(reader);

            if (obj is null)
                throw new InvalidDataException();

            return obj;
        }

        private static async Task<ApiLinks> FetchApiLinks(HttpClient hc)
        {
            return FromString<ApiLinks>(await FetchWithFallback(hc, new Uri(APILINKS_URI), new Uri(FALLBACK_APILINKS_URI)));
        }
        
        private static async Task<ModLinks> FetchModLinks(HttpClient hc)
        {
            return FromString<ModLinks>(await FetchWithFallback(hc, new Uri(MODLINKS_URI), new Uri(FALLBACK_MODLINKS_URI)));
        }

        private static async Task<string> FetchWithFallback(HttpClient hc, Uri uri, Uri fallback)
        {
            try
            {
                var cts = new CancellationTokenSource(10000);
                return await hc.GetStringAsync(uri, cts.Token);
            }
            catch (Exception e) when (e is TaskCanceledException or HttpRequestException)
            {
                var cts = new CancellationTokenSource(10000);
                return await hc.GetStringAsync(fallback, cts.Token);
            }
        }
    }
}