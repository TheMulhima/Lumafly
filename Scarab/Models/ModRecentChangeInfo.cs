using System;
using Scarab.Enums;

namespace Scarab.Models;

/// <summary>
/// We get 2 modlinks from 30 days and 7 days ago. Then we have to compare to current and see
/// 1. If it isn't present in old
/// 2. If the version is different than the old
/// If both happens we just use created. 

/// Also we will use https://github.com/Clazex/HKModLinksHistory/tree/dist to get a sorting order by date so we need
/// to account for that too 
/// </summary>
public class ModRecentChangeInfo : IComparable<ModRecentChangeInfo>
{
    public ModChangedSorterInfo? SorterInfo;
    public ModChangeState ChangeState;

    public ModRecentChangeInfo()
    {
        ChangeState = ModChangeState.None;
    }

    public void AddChanges(ModChangeState changeState, DateTime lastChanged)
    {
        // this is to account for if a mod is created and updated within a month. if that happens, only count it as 
        // created. This will do it for us as Created value is 2 while Updated is 1 in the enum
        if (changeState > ChangeState)
        {
            ChangeState = changeState;
            SorterInfo = new ModChangedSorterInfo(lastChanged);
        }
        else if (changeState == ChangeState)
        {
            // Keep the most recent change (if updated last week and last month, keep only last week)
            if (SorterInfo is null || SorterInfo.LastChanged < lastChanged)
            {
                SorterInfo = new ModChangedSorterInfo(lastChanged);
            }
        }
    }
    
    public void AddSortOrder(int sortOrder)
    {
        if (ChangeState != ModChangeState.None)
        {
            SorterInfo?.AddSortOrder(sortOrder);
        }
    }
    
    public bool IsCreatedRecently => ChangeState == ModChangeState.Created;
    public bool IsUpdatedRecently => ChangeState == ModChangeState.Updated;

    public bool ShouldShowUp(ModChangeState state, bool week)
    {
        if (state == ModChangeState.Created && IsCreatedRecently)
        {
            var lastCreated = SorterInfo?.LastChanged ?? throw new InvalidOperationException();
            return lastCreated > DateTime.UtcNow.AddDays(-1 * (week ? 8 : 31));
        }

        if (state == ModChangeState.Updated && IsUpdatedRecently)
        {
            var lastUpdated = SorterInfo?.LastChanged ?? throw new InvalidOperationException();
            return lastUpdated > DateTime.UtcNow.AddDays(-1 * (week ? 8 : 31));
        }
        return false; 
    }

    public int CompareTo(ModRecentChangeInfo? other)
    {
        if (other == null) return 1;
        if (this == other) return 0;

        if (SorterInfo == null && other.SorterInfo == null) return 0;
        if (SorterInfo != null && other.SorterInfo == null) return 1;
        if (SorterInfo == null && other.SorterInfo != null) return -1;
        
        return SorterInfo!.CompareTo(other.SorterInfo);
    }
}

public class ModChangedSorterInfo : IComparable<ModChangedSorterInfo>
{
    public DateTime LastChanged { get; }
    public int? SortOrder { get; private set; }

    public ModChangedSorterInfo(DateTime lastChanged)
    {
        LastChanged = lastChanged;
        SortOrder = null;
    }

    public void AddSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }

    public int CompareTo(ModChangedSorterInfo? other)
    {
        if (other == null) return 1;
        
        // shouldn't be needed but just in case
        if (SortOrder.HasValue && !other.SortOrder.HasValue) return 1;
        if (!SortOrder.HasValue && other.SortOrder.HasValue) return -1;
        
        // if sort order exists, use that else use date to sort
        if (SortOrder.HasValue && other.SortOrder.HasValue) 
            return SortOrder.Value.CompareTo(other.SortOrder);
        
        return LastChanged.CompareTo(other.LastChanged);
    }
}