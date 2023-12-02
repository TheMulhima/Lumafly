using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using Lumafly.Enums;
using Lumafly.Interfaces;
using Lumafly.Models;
using Lumafly.Util;

namespace Lumafly.Services;

/// <summary>
/// Gets the changed mods lists from https://github.com/Clazex/HKModLinksHistory
/// </summary>
public class ModLinksChanges : IModLinksChanges
{
    private readonly ISettings settings;
    private readonly LumaflyMode lumaflyMode;
    private readonly IEnumerable<ModItem> currentItems;
    
    /// <summary>
    /// TriState variable to decide if whats new should be displayed.
    /// Default (null) means its not loaded yet
    /// True means it has successfully loaded
    /// False means something has failed and nothing should be displayed as it would be undefined behaviour 
    /// </summary>
    public bool? IsLoaded { get; private set; }

    public ModLinksChanges(IEnumerable<ModItem> _items, ISettings _settings, LumaflyMode _lumaflyMode)
    {
        currentItems = _items;
        settings = _settings;
        lumaflyMode = _lumaflyMode;
        Trace.WriteLine("ModlinksChanges service created");
    }
    
    /// <summary>
    /// Actually runs the service. Needs to be called in <see cref="ViewModels.ModListViewModel"/> because after its
    /// completion, we need raise property changed and reselect the mods which we can't access here.
    /// </summary>
    public async Task LoadChanges()
    {
        // only do it if its not offline and lumafly is using hk-modding/modlinks as its modlinks provider
        if (lumaflyMode == LumaflyMode.Offline ||
            settings.UseCustomModlinks)
        {
            IsLoaded = false;
            return;
        }
        
        var result = await WorkaroundHttpClient.TryWithWorkaroundAsync(
            settings.RequiresWorkaroundClient 
                ? HttpSetting.OnlyWorkaround
                : HttpSetting.TryBoth,
            FetchContent,
            AddHttpConfig
        );
        
        IsLoaded = result.Result;
    }

    /// <summary>
    /// Fetches all the content we need for this service
    /// </summary>
    /// <returns>The successfulness of the fetch operation</returns>
    private async Task<bool> FetchContent(HttpClient hc)
    {
        // on any error we wont display anything. Its not really worth error handling as this isn't necessary feature

        var links = await GetLinks(hc);

        if (links == null) return false;

        var fetch_new_week = await GetAndUpdateRecentChangeInfo(hc, links.Value.new_week, ModChangeState.New, HowRecentModChanged.Week);
        var fetch_new_month = await GetAndUpdateRecentChangeInfo(hc, links.Value.new_month, ModChangeState.New, HowRecentModChanged.Month);
        var fetch_updated_week = await GetAndUpdateRecentChangeInfo(hc, links.Value.updated_week, ModChangeState.Updated, HowRecentModChanged.Week);
        var fetch_updated_month = await GetAndUpdateRecentChangeInfo(hc, links.Value.updated_month, ModChangeState.Updated, HowRecentModChanged.Month);
        
        // we don't need to worry about concurrent running tasks throwing unhandled errors as the function will not throw 
        var success = new List<bool>
        {
            fetch_new_week,
            fetch_new_month,
            fetch_updated_week,
            fetch_updated_month,
        };
        
        // fetch sorting after all others finished
        var fetch_sortOrder = await GetAndUpdateSortOrder(hc, links.Value.sortOrder);
        success.Add(fetch_sortOrder);

        // get mods that were changed by comparing the last version of Modlinks.xml used by HKModLinksHistory
        // no need to verify the success of this
        await GetWithinTodayChanges(hc, links.Value.lastUsedModlinks);

        // only make IsLoaded true if all are true
        return success.All(x => x);
    }
    
    /// <summary>
    /// Get links from the json in static resources branch
    /// </summary>
    private async Task<ChangesLinks?> GetLinks(HttpClient hc)
    {
        try
        {
            JsonDocument linkJson = JsonDocument.Parse(
                await hc.GetStringAsync2(
                    settings,
                    new Uri("https://raw.githubusercontent.com/TheMulhima/Lumafly/static-resources/ModlinksChanges.json"), 
                    new CancellationTokenSource(ModDatabase.TIMEOUT).Token));

            string GetPropertyFromDocument(string keyName)
            {
                return linkJson.RootElement.GetProperty(keyName).GetString() 
                       ?? throw new Exception($"{keyName} not found in links json");
            }
            
            ChangesLinks result = new (
                GetPropertyFromDocument(nameof(result.new_week)),
                GetPropertyFromDocument(nameof(result.new_month)),
                GetPropertyFromDocument(nameof(result.updated_week)),
                GetPropertyFromDocument(nameof(result.updated_month)),
                GetPropertyFromDocument(nameof(result.sortOrder)),
                GetPropertyFromDocument(nameof(result.lastUsedModlinks))
                );

            return result;
        }
        catch(Exception e)
        {
            Trace.WriteLine($"An exception occured when trying to get links for modlinks changes: {e}");
            return null;
        }
    }

    /// <summary>
    /// Get the changed mod list, get the corresponding mod and update their RecentChangeInfo
    /// </summary>
    /// <returns>The successfulness of the task</returns>
    private async Task<bool> GetAndUpdateRecentChangeInfo(HttpClient hc, string link, ModChangeState changeState, HowRecentModChanged howRecentModChanged)
    {
        try
        {
            var modNamesString = await hc.GetStringAsync2(
                settings,
                new Uri(link), 
                new CancellationTokenSource(ModDatabase.TIMEOUT).Token);

            var modList = modNamesString
                .Split('\n')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Where(x => currentItems.Any(mod => x == mod.Name))
                .Select(x => currentItems.First(y => y.Name == x));
            
            foreach (var mod in modList)
            {
                mod.RecentChangeInfo.AddChanges(changeState, howRecentModChanged);
            }

            return true;
        }
        catch(Exception e)
        {
            Trace.WriteLine($"An exception occured when trying to get the modlinks changes: {e}" );
            return false;
        } 
    }
    
    /// <summary>
    /// Get the last modlinks.xml file used by HKModLinksHistory and compare it with current modlinks to get todays changes
    /// </summary>
    /// <returns>The successfulness of the task</returns>
    private async Task<bool> GetWithinTodayChanges(HttpClient hc, string lastUsedModlinks)
    {
        try
        {
            var oldModlinks = ModDatabase.FromString<ModLinks>(await hc.GetStringAsync2(
                settings,
                lastUsedModlinks, 
                new CancellationTokenSource(ModDatabase.TIMEOUT).Token));

            foreach (var mod in currentItems.Where(x => x.State is not NotInModLinksState { ModlinksMod: false }))
            {
                var correspondingOldMod = oldModlinks.Manifests.FirstOrDefault(m => m.Name == mod.Name);
                if (correspondingOldMod is not null)
                {
                    if (correspondingOldMod.Version.Value != mod.Version)
                    {
                        mod.RecentChangeInfo.AddChanges(ModChangeState.Updated, HowRecentModChanged.Week);
                        mod.RecentChangeInfo.AddSortOrder(-1); // not going to bother sorting further should just appear at the top
                    }
                }
                else
                {
                    mod.RecentChangeInfo.AddChanges(ModChangeState.New, HowRecentModChanged.Week);
                    mod.RecentChangeInfo.AddSortOrder(-1); // not going to bother sorting further should just appear at the top
                }
            }
            
            return true;
        }
        catch(Exception e)
        {
            Trace.WriteLine($"An exception occured when trying to get the modlinks changes: {e}" );
            return false;
        }
    }
    
    /// <summary>
    /// Get the sort order from ChangedMods-month which will be used to sort instead of alphabetical
    /// </summary>
    /// <returns>The successfulness of the task</returns>
    private async Task<bool> GetAndUpdateSortOrder(HttpClient hc, string link)
    {
        try
        {
            var modNamesString = await hc.GetStringAsync2(
                settings,
                new Uri(link), 
                new CancellationTokenSource(ModDatabase.TIMEOUT).Token);

            var modNamesList = modNamesString
                .Split('\n')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Where(x => currentItems.Any(mod => x == mod.Name))
                .ToList();

            // it gives us the opposite order we need
            modNamesList.Reverse();
            
            // remove all but the first instance of each mod
            var sortedModNamesList = new HashSet<string>(modNamesList);
            
            // remove mods that are not in this list to prevent undefined behaviour
            foreach (var item in currentItems)
            {
                if (item.RecentChangeInfo.ChangeState != ModChangeState.None && !sortedModNamesList.Contains(item.Name))
                {
                    item.RecentChangeInfo = new ModRecentChangeInfo();
                }
            }

            foreach (var mod in sortedModNamesList.Select(x => currentItems.First(y => y.Name == x)))
            {
                mod.RecentChangeInfo.AddSortOrder(sortedModNamesList.IndexOf(mod.Name));
            }
        }
        catch(Exception e)
        {
            Trace.WriteLine($"An exception occured when trying to get the modlinks changes: {e}" );
            return false;
        }

        return true;
    }

    private void AddHttpConfig(HttpClient hc)
    {
        hc.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
        {
            NoCache = false,
            MustRevalidate = false
        };
                
        hc.DefaultRequestHeaders.Add("User-Agent", "Lumafly");
    }

    private record struct ChangesLinks(
        string new_week,
        string new_month,
        string updated_week,
        string updated_month,
        string sortOrder,
        string lastUsedModlinks);
}