using System.Collections.Generic;
using System.Threading.Tasks;
using Scarab.Models;
using Scarab.Util;

namespace Scarab.Interfaces;

public interface IPackManager
{
   public SortableObservableCollection<Pack> PackList { get; }
   
   Task LoadPack(string packName);

   Task SavePack(string name, string description);

   void RemovePack(string packName);
}