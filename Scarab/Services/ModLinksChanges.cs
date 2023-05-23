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
using DynamicData.Kernel;
using Scarab.Enums;
using Scarab.Interfaces;
using Scarab.Models;
using Scarab.Util;

namespace Scarab.Services;

public class ModLinksChanges : IModLinksChanges
{
    private readonly ISettings settings;
    private readonly ScarabMode scarabMode;
    private readonly IEnumerable<ModItem> currentItems;
    public bool? IsReady { get; set; }

    public ModLinksChanges(IEnumerable<ModItem> _items, ISettings _settings, ScarabMode _scarabMode)
    {
        currentItems = _items;
        settings = _settings;
        scarabMode = _scarabMode;
    }
    
    public async Task LoadChanges()
    {
        if (scarabMode == ScarabMode.Offline ||
            settings.BaseLink != ModDatabase.DEFAULT_LINKS_BASE ||
            settings.UseCustomModlinks)
        {
            IsReady = false;
            return;
        }
        
        IsReady = await GetOldModlinks();
    }

    private async Task<bool> GetOldModlinks()
    {
        var res = await WorkaroundHttpClient.TryWithWorkaroundAsync(
            settings.RequiresWorkaroundClient 
                ? HttpSetting.OnlyWorkaround
                : HttpSetting.TryBoth,
            FetchContent,
            AddHttpConfig
        );

        return res.Result.All(x => x);
    }
    
    private async Task<bool[]> FetchContent(HttpClient hc)
    {
        var oneWeekOld = GetModListDifference(hc, DateTime.UtcNow.AddDays(-7));
        var oneMonthOld = GetModListDifference(hc, DateTime.UtcNow.AddDays(-30));

        var success = new[]
        {
            await oneWeekOld,
            await oneMonthOld
        };
        
        // we don't really care about the success of the sort fetch.
        // if we didn't get it we can just sort alphabetically not the end of the world
        await GetSortOrder(hc);
        
        return success;
    }

    private async Task<bool> GetModListDifference(HttpClient hc, DateTime timeToGet)
    {
        try
        {
            JsonDocument result = JsonDocument.Parse(
                await hc.GetStringAsync(
                    new Uri($"https://api.github.com/repos/hk-modding/modlinks/commits?since={timeToGet:s}Z&per_page=100"), 
                    new CancellationTokenSource(ModDatabase.TIMEOUT).Token));
            
            // we will get back an array of commits so we tell that to the JsonDocument
            var commits = result.RootElement.EnumerateArray();
            
            if (!commits.Any()) return false;

            var commit = commits.Last();
            var sha = commit.GetProperty("sha").GetString();

            if (string.IsNullOrEmpty(sha)) return false;
            
            var oldModlinks = ModDatabase.FromString<ModLinks>(await hc.GetStringAsync(ModDatabase.GetModlinksUri(settings, sha), 
                new CancellationTokenSource(ModDatabase.TIMEOUT).Token));
            
            var commitDate = commit
                .GetProperty("commit")
                .GetProperty("committer")
                .GetProperty("date")
                .GetDateTime();
            
            foreach (var mod in currentItems.Where(x => x.State is not NotInModLinksState { ModlinksMod: false }))
            {
                var correspondingOldMod = oldModlinks.Manifests.FirstOrDefault(m => m.Name == mod.Name);

                if (correspondingOldMod is not null)
                {
                    if (correspondingOldMod.Version.Value < mod.Version)
                    {
                        mod.RecentChangeInfo.AddChanges(ModChangeState.Updated, commitDate);
                    }
                }
                else
                {
                    mod.RecentChangeInfo.AddChanges(ModChangeState.Created, commitDate);
                }
            }
        }
        catch(Exception e)
        {
            // we aren't going to retry because its not important to get this list
            // also its possible that reopening scarab many times might lead to rate limiting
            // so this and the latest version check wont work
            Trace.WriteLine($"An exception occured when trying to get the modlinks changes: {e}" );
            return false;
        }

        return true;
    }
    
    // we need to get the sort order like this because to get the real order, we need to make 30 requests to github
    // but thats too many points of failure and we only have 60 requests to github api per hour. So we use
    // https://github.com/Clazex/HKModLinksHistory which runs once per day
    private async Task GetSortOrder(HttpClient hc)
    {
        try
        {
            JsonDocument linkJson = JsonDocument.Parse(
                await hc.GetStringAsync(
                    new Uri($"https://raw.githubusercontent.com/TheMulhima/Scarab/static-resources/ModlinksChanges.json"), 
                    new CancellationTokenSource(ModDatabase.TIMEOUT).Token));
            
            // we only need the months list as the weeks link is basically the end of months list
            var sortedListLink = linkJson.RootElement.GetProperty("month").GetString();
            
            if (sortedListLink is null) return;
            
            var sortedList = (await hc.GetStringAsync(sortedListLink, 
                new CancellationTokenSource(ModDatabase.TIMEOUT).Token)).Trim().Split('\n').ToList();
            
            // it gives opposite order so we reverse
            sortedList.Reverse();
            
            // remove all but the first instance of each mod
            var sortedSet = new HashSet<string>(sortedList);

            // remove false positives
            foreach (var item in currentItems)
            {
                if (item.RecentChangeInfo.ChangeState != ModChangeState.None && !sortedSet.Contains(item.Name))
                {
                    item.RecentChangeInfo = new ModRecentChangeInfo();
                }
            }

            foreach (var mod in sortedSet.Select(item => 
                         currentItems.FirstOrDefault(x => x.Name == item)))
            {
                mod?.RecentChangeInfo.AddSortOrder(sortedSet.IndexOf(mod.Name));
            }
        }
        catch(Exception e)
        {
            // the feature still works without sort order cuz we can sort alphabetically
            Trace.WriteLine($"An exception occured when trying to get sort order: {e}" );
        }
    }

    private void AddHttpConfig(HttpClient hc)
    {
        hc.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
        {
            NoCache = false,
            MustRevalidate = false
        };
                
        hc.DefaultRequestHeaders.Add("User-Agent", "Scarab");
    }
}