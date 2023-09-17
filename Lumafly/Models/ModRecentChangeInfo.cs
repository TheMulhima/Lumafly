using System;
using System.Diagnostics;
using Lumafly.Enums;

namespace Lumafly.Models;

/// <summary>
/// Stores the change info for us to display whats new tab
/// If a mod is updated and created, only counts as created.
/// </summary>
public class ModRecentChangeInfo
{
    public int SortOrder;
    public ModChangeState ChangeState;
    public HowRecentModChanged HowRecentModChanged;

    public ModRecentChangeInfo()
    {
        ChangeState = ModChangeState.None;
    }

    public void AddChanges(ModChangeState changeState, HowRecentModChanged howRecentModChanged)
    {
        // this is to account for if a mod is created and updated. if that happens, only count it as 
        // created. This will do it for us as created's enum value is bigger than updated
        if (changeState > ChangeState)
        {
            ChangeState = changeState;
            HowRecentModChanged = howRecentModChanged;
        }
        else if (changeState == ChangeState)
        {
            // if the mod was changed earlier, update it (month has smaller enum value than week)
            if (howRecentModChanged > HowRecentModChanged)
            {
                HowRecentModChanged = howRecentModChanged;
            }
        }
    }
    
    public void AddSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }

    public bool IsCreatedRecently => ChangeState == ModChangeState.New;
    public bool IsUpdatedRecently => ChangeState == ModChangeState.Updated;

    public bool ShouldBeShown(ModChangeState state, HowRecentModChanged howRecentModChanged)
    {
        return state switch
        {
            ModChangeState.New when IsCreatedRecently => HowRecentModChanged >= howRecentModChanged,
            ModChangeState.Updated when IsUpdatedRecently => HowRecentModChanged >= howRecentModChanged,
            _ => false
        };
    }

    public int CompareTo(ModRecentChangeInfo? other)
    {
        return other == null ? 1 : SortOrder.CompareTo(other.SortOrder);
    }
}