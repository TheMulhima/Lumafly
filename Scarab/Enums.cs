namespace Scarab
{
    public enum AutoRemoveUnusedDepsOptions
    {
        Never = 0,
        Ask = 1,
        Always = 2,
    }
    
    public enum ScarabMode
    {
        Online,
        Offline
    }
    
    public enum ModChangeState
    {
        Created,
        Updated
    }
    
    public enum HttpSetting
    {
        OnlyWorkaround,
        TryBoth
    }
    
    public enum UrlSchemeCommands
    {
        none,
        download,
        reset,
        forceUpdateAll,
        customModLinks,
        baseLink,
    }
}
