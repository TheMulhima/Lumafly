using System;
using Avalonia.Controls;
using Scarab.Interfaces;
using Scarab.Models;
using Scarab.Util;

namespace Scarab.ViewModels;

public class PackManagerViewModel : ViewModelBase
{
    private readonly IPackManager _packManager;
    
    public SortableObservableCollection<Pack> Packs => _packManager.PackList;

    public PackManagerViewModel(IPackManager packManager)
    {
        _packManager = packManager;
    }

    public void GenerateSharingCode(object packObj)
    {
        var pack = packObj as Pack ?? throw new InvalidOperationException("Cannot share an object that is not a pack");
        //TODO: implement
    }
    
    public void EditPack(object packObj)
    {
        var pack = packObj as Pack ?? throw new InvalidOperationException("Cannot edit an object that is not a pack");
        //TODO: implement
    }
    
    public void DeletePack(object packObj)
    {
        var pack = packObj as Pack ?? throw new InvalidOperationException("Cannot delete an object that is not a pack");
        _packManager.RemovePack(pack.Name);
    }
    
    public void LoadPack(object packObj)
    {
        var pack = packObj as Pack ?? throw new InvalidOperationException("Cannot delete an object that is not a pack");
        _packManager.LoadPack(pack.Name);
        MainWindowViewModel.Instance?.LoadApp(2);
    }
    
    public void CreateNewPack()
    {
        _packManager.SavePack("New Pack " + new Random().NextInt64(2000), "New Pack Description");
    }
    
    public void ImportPack()
    {
        //TODO: implement
    }
    
    public void CopySharingCode(object packObj)
    {
        var pack = packObj as Pack ?? throw new InvalidOperationException("Cannot copy an object that is not a pack");
        
        TopLevel.GetTopLevel(AvaloniaUtils.GetMainWindow())?.Clipboard?.SetTextAsync(pack.SharingCode);
    }

    public void SavePackToZip(object packObj)
    {
        var pack = packObj as Pack ?? throw new InvalidOperationException("Cannot share an object that is not a pack");
        _packManager.SavePackToZip(pack.Name);
    }
}