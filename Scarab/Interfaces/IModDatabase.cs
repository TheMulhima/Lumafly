using System.Collections.Generic;
using Scarab.Models;

namespace Scarab.Interfaces
{
    public interface IModDatabase
    {
        List<ModItem> Items { get; }
        
        (string Url, int Version, string SHA256) Api { get; }
    }
}