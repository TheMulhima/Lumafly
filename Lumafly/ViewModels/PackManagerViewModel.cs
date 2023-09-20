using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using PropertyChanged.SourceGenerator;
using Lumafly.Interfaces;
using Lumafly.Models;
using Lumafly.Util;
using Lumafly.Enums;
using Lumafly.Views.Windows;
using MsBox.Avalonia.Enums;

namespace Lumafly.ViewModels;

public partial class PackManagerViewModel : ViewModelBase
{
    private readonly IPackManager _packManager;
    private readonly IUrlSchemeHandler _urlSchemeHandler;

    [Notify] private bool _loadingSharingCode;
    
    public event Action<string>? OnPackLoaded;
    public event Action<string>? OnPackImported;
    
    public SortableObservableCollection<Pack> Packs => _packManager.PackList;

    public PackManagerViewModel(IPackManager packManager, IUrlSchemeHandler urlSchemeHandler)
    {
        _packManager = packManager;
        _urlSchemeHandler = urlSchemeHandler;

        Dispatcher.UIThread.Invoke(HandleModPackUrlScheme);
    }

    public async Task GenerateSharingCode(object packObj)
    {
        var pack = packObj as Pack ?? throw new InvalidOperationException("Cannot share an object that is not a pack");
        try
        {
            LoadingSharingCode = true;
            await _packManager.UploadPack(pack.Name);
            if (pack.SharingCode != null) RaisePropertyChanged(pack.SharingCode);
        }
        catch (HttpRequestException e)
        {
            await DisplayErrors.DisplayNetworkError(pack.Name, e);
        }
        catch (Exception e)
        {
            await DisplayErrors.DisplayGenericError("Failed to generate sharing code", e);
        }
        finally
        {
            LoadingSharingCode = false;
        }
    }
    
    public void EditPack(object packObj)
    {
        var pack = packObj as Pack ?? throw new InvalidOperationException("Cannot edit an object that is not a pack");
        _packManager.EditPack(pack);
    }
    
    public void DeletePack(object packObj)
    {
        var pack = packObj as Pack ?? throw new InvalidOperationException("Cannot delete an object that is not a pack");
        _packManager.RemovePack(pack.Name);
    }
    
    public async Task LoadPack(object packObj)
    {
        var pack = packObj as Pack ?? throw new InvalidOperationException("Cannot load an object that is not a pack");

        bool success = false;
        
        await Dispatcher.UIThread.InvokeAsync(
            async () => await MainWindowViewModel.Instance!.LoadApp(2,
            new LoadingTaskDetails(
                async () =>
                {
                    try
                    {
                        success = await _packManager.LoadPack(pack.Name);
                    }
                    catch (Exception e)
                    {
                        await DisplayErrors.DisplayGenericError("Failed to load pack", e);
                    }
                },
                "Loading Pack")));

        if (success) 
            OnPackLoaded?.Invoke(pack.Name);
    }
    
    public void CreateNewPack()
    {
        _packManager.SavePack("New Pack " + new Random().NextInt64(2000), "New Pack", "");
    }
    
    public async Task ImportPack()
    {
        var importPackPopup = new ImportPackPopup()
        {
            DataContext = new ImportPackPopupViewModel()
        };

        var code = await importPackPopup.ShowDialog<string>(AvaloniaUtils.GetMainWindow());

        if (string.IsNullOrEmpty(code))
            return;
        
        Pack? importedPack = await _packManager.ImportPack(code);
        
        if (importedPack != null) 
            await LoadPack(importedPack);
    }
    
    public void CopySharingCode(object packObj)
    {
        var pack = packObj as Pack ?? throw new InvalidOperationException("Cannot copy an object that is not a pack");
        
        TopLevel.GetTopLevel(AvaloniaUtils.GetMainWindow())?.Clipboard?.SetTextAsync(ImportPackPopupViewModel.WebsiteShareLink + pack.SharingCode);
    }

    public void SavePackToZip(object packObj)
    {
        var pack = packObj as Pack ?? throw new InvalidOperationException("Cannot share an object that is not a pack");
        _packManager.SavePackToZip(pack.Name);
    }
    
    private async Task HandleModPackUrlScheme()
    {
        if (_urlSchemeHandler is { Handled: false, UrlSchemeCommand: UrlSchemeCommands.modpack })
        {
            Pack? importedPack = await _packManager.ImportPack(_urlSchemeHandler.Data);

            if (importedPack != null)
            {
                MainWindowViewModel.Instance!.SelectedTabIndex = 2;
                await Task.Delay(250);
                OnPackImported?.Invoke(importedPack.Name);
            }
            
            _urlSchemeHandler.FinishHandlingUrlScheme();
        }
    }
}