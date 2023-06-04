using System;
using System.Collections.Generic;
using System.Linq;
using Scarab.Interfaces;
using Scarab.Models;

namespace Scarab.Mock;

public class MockDatabase : IModDatabase
{
    public MockDatabase()
    {
        Api = ("...", 256, "?");

        Items = new ModItem[]
        {
            // Installed and up to date
            new
            (
                new InstalledState(true, new Version(1, 0), true),
                new Version(1, 0),
                Array.Empty<string>(),
                "link",
                "sha",
                "NormalEx",
                "An example", 
                "github.com/fifty-six/no",
                Array.Empty<string>(),
                Array.Empty<string>()
            ),
            // Installed but out of date
            new
            (
                new InstalledState(true, new Version(1, 0), false),
                new Version(2, 0),
                Array.Empty<string>(),
                "link",
                "sha",
                "OutOfDateEx",
                "An example",
                "https://github.com/fifty-six/yup",
                Array.Empty<string>(),
                Array.Empty<string>()
            ),
            // Not installed
            new
            (
                new NotInstalledState(),
                new Version(1, 0),
                Array.Empty<string>(),
                "link",
                "sha",
                "NotInstalledEx",
                "An example",
                "example.com",
                Array.Empty<string>(),
                Array.Empty<string>()
            ),
            // Example with a really long name and tags and integrations
            new
            (
                new NotInstalledState(),
                new Version(1, 0),
                Array.Empty<string>(),
                "link",
                "sha",
                string.Join("", Enumerable.Repeat("Very", 8)) + "LongModName",
                "An example",
                "https://example.com/really/really/really/really/really/long/url/to/test/wrapping/impls/....",
                new[] { "Tag1", "Tag2", "Tag3" },
                new[] { "NormalEx" }
            )
        };
    }

    public IEnumerable<ModItem>                     Items { get; }
    public (string Url, int Version, string SHA256) Api   { get; }
}