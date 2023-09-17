namespace Lumafly.Models;

public record struct ModProgressArgs
{
    public DownloadProgressArgs? Download  { get; internal set; }
    public bool                  Completed { get; internal set; }
}