using System.Collections.Generic;
using System.Threading.Tasks;
using Lumafly.Models;
using Lumafly.Util;

namespace Lumafly.Interfaces;

public interface IPackManager
{
   public SortableObservableCollection<Pack> PackList { get; }
   
   Task<bool> LoadPack(string packName, bool additive);

   Task SavePack(string name, string description, string authors);

   void RemovePack(string packName);

   void SavePackToZip(string packName);
   Task EditPack(Pack pack);

   Task UploadPack(string packName);

   Task<Pack?> ImportPack(string code);

   Task RevertToPreviousModsFolder();
}