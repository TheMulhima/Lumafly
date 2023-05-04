using Scarab.Models;
using System.Collections.Generic;

namespace Scarab.Interfaces
{
    public interface IProfileDatabase
    {
        List<Profile> Items { get; }
    }
}
