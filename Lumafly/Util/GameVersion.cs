using System;

namespace Lumafly.Util;

public static class GameVersion
{
    /// <remarks>
    /// Constants.GAME_VERSION has no fixed component count ("1.5.78.11833", "1.5.12620"),
    /// so missing components compare as 0.
    /// </remarks>
    public static bool Equal(string? a, string? b)
    {
        if (a is null || b is null)
            return false;

        return Normalize(a) is { } x && Normalize(b) is { } y ? x == y : a == b;
    }

    public static Version? Normalize(string? version) =>
        Version.TryParse(version, out var v)
            ? new Version(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0))
            : null;
}
