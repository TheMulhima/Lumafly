using System;
using System.Collections.Generic;

namespace Scarab.Models;

public class ModRecentChangeInfo
{
    Dictionary<ModChangeState, DateTime> ModChanges { get; } = new();

    public ModRecentChangeInfo() { }
    
    public ModRecentChangeInfo(params (ModChangeState, DateTime)[] changes) : this()
    {
        AddChanges(changes);
    }
    
    public void AddChanges(params (ModChangeState, DateTime)[] changes)
    {
        foreach (var change in changes)
        {
            if (ModChanges.TryGetValue(change.Item1, out var date))
            {
                if (change.Item2 > date)
                {
                    ModChanges[change.Item1] = change.Item2;
                }
            }
            else
            {
                ModChanges.Add(change.Item1, change.Item2);
            }
        }
    }
    
    public bool IsCreatedRecently => ModChanges.ContainsKey(ModChangeState.Created);
    public bool IsUpdatedRecently => ModChanges.ContainsKey(ModChangeState.Updated);
    
    public DateTime LastCreated => ModChanges.TryGetValue(ModChangeState.Created, out var date) ? date : DateTime.MinValue;
    public DateTime LastUpdated => ModChanges.TryGetValue(ModChangeState.Updated, out var date) ? date : DateTime.MinValue;
}