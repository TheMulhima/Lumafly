using System.Collections.Generic;
using System.Threading.Tasks;
using Scarab.Models;

namespace Scarab.Interfaces;

public interface IPackManager
{
   public IEnumerable<Pack> PackList { get; }
   
   Task LoadPack(string packName);

   Task SavePack(string name, string description);

   void RemovePack(string packName);
}