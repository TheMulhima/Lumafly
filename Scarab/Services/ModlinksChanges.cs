using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Scarab.Interfaces;
using Scarab.Models;
using Scarab.Util;

namespace Scarab.Services;

public class ModlinksChanges
{
    private readonly ISettings settings;
    private readonly IEnumerable<ModItem> _currentItems;
    public bool? IsReady;

    public ModlinksChanges(IEnumerable<ModItem> _items, ISettings _settings)
    {
        _currentItems = _items;
        settings = _settings;
        Task.Run(GetOldModlinks);
    }

    private async Task GetOldModlinks()
    {
        var res = await WorkaroundHttpClient.TryWithWorkaroundAsync(
            settings.RequiresWorkaroundClient 
                ? WorkaroundHttpClient.Settings.OnlyWorkaround
                : WorkaroundHttpClient.Settings.TryBoth,
            FetchContent,
            AddHttpConfig
        );

        IsReady = res.Result.All(x => x);
    }
    
    private async Task<bool[]> FetchContent(HttpClient hc)
    {
        var oneWeekOld = GetModListDifference(hc, DateTime.UtcNow.AddDays(-7));
        var oneMonthOld = GetModListDifference(hc, DateTime.UtcNow.AddMonths(-1));

        return new[]
        {
            await oneWeekOld,
            await oneMonthOld
        };
    }

    private async Task<bool> GetModListDifference(HttpClient hc, DateTime timeToGet)
    {
        try
        {
            JsonDocument result = JsonDocument.Parse(
                await hc.GetStringAsync(
                    new Uri($"https://api.github.com/repos/hk-modding/modlinks/commits?since={timeToGet:s}Z&per_page=100")));
            
            // we will get back an array of commits so we tell that to the JsonDocument
            var commits = result.RootElement.EnumerateArray();
            
            if (!commits.Any()) return false;

            var commit = commits.Last();
            var sha = commit.GetProperty("sha").GetString();

            if (string.IsNullOrEmpty(sha)) return false;
            
            var cts = new CancellationTokenSource(ModDatabase.TIMEOUT);
            var oldModlinks = ModDatabase.FromString<ModLinks>(await hc.GetStringAsync(ModDatabase.GetModlinksUri(sha), cts.Token));

            foreach (var mod in _currentItems.Where(x => x.State is not NotInModLinksState))
            {
                var correspondingOldMod = oldModlinks.Manifests.FirstOrDefault(m => m.Name == mod.Name);
                
                var commitDate = commit
                    .GetProperty("commit")
                    .GetProperty("committer")
                    .GetProperty("date")
                    .GetDateTime();
                
                if (correspondingOldMod is not null)
                {
                    if (correspondingOldMod.Version.Value < mod.Version)
                    {
                        // use most recent last changed date
                        mod.LastChanged = mod.LastChanged > commitDate
                            ? mod.LastChanged
                            : commitDate;
                        
                        mod.ModChangeState = ModChangeState.Updated;
                    }
                }
                else
                {
                    mod.LastChanged = mod.LastChanged > commitDate
                        ? mod.LastChanged
                        : commitDate;
                    mod.ModChangeState = ModChangeState.Created;
                }
            }
        }
        catch(Exception e)
        {
            // we aren't going to retry because its not important to get this list
            Trace.WriteLine($"An exception occured when trying to get the modlinks changes: {e}" );
            return false;
        }

        return true;
    }

    private void AddHttpConfig(HttpClient hc)
    {
        hc.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true,
            MustRevalidate = true
        };
                
        hc.DefaultRequestHeaders.Add("User-Agent", "Scarab");
    }
}