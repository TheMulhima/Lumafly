using System.Collections.Generic;
using Lumafly.Models;

namespace Lumafly.Interfaces
{
    public interface IModDatabase
    {
        List<ModItem> Items { get; }
        
        (string Url, int Version, string SHA256) Api { get; }
    }
}