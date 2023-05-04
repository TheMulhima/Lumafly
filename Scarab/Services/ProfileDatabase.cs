using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using JetBrains.Annotations;
using Scarab.Interfaces;
using Scarab.Models;

namespace Scarab.Services
{
    public class ProfileDatabase : IProfileDatabase
    {
        [UsedImplicitly]
        public List<Profile> Items { get; }

        public ProfileDatabase(ISettings settings)
        {
            Items = new List<Profile>(settings.Profiles);
            Items.Sort((a, b) => string.Compare(a.Name, b.Name, true, CultureInfo.InvariantCulture));
        }
    }
}
