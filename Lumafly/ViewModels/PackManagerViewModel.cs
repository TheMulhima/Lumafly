using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using PropertyChanged.SourceGenerator;
using Lumafly.Interfaces;
using Lumafly.Models;
using Lumafly.Util;
using Lumafly.Enums;
using Lumafly.Services;
using Lumafly.Views.Windows;
using MsBox.Avalonia.Enums;

namespace Lumafly.ViewModels;

public partial class PackManagerViewModel : ViewModelBase
{
    private readonly IPackManager _packManager;
    private readonly IUrlSchemeHandler _urlSchemeHandler;
    private readonly ISettings _settings;

    [Notify] private bool _loadingSharingCode;
    
    public event Action<string>? OnPackLoaded;
    public event Action<string>? OnPackImported;
    
    public SortableObservableCollection<Pack> Packs => _packManager.PackList;

    public PackManagerViewModel(IPackManager packManager, IUrlSchemeHandler urlSchemeHandler, ISettings settings)
    {
        _packManager = packManager;
        _urlSchemeHandler = urlSchemeHandler;
        _settings = settings;

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

    public async Task RevertToPreviousModsFolder()
    {
        await Dispatcher.UIThread.InvokeAsync(
            async () => await MainWindowViewModel.Instance!.LoadApp(2,
                new LoadingTaskDetails(
                    async () =>
                    {
                        try
                        {
                            await _packManager.RevertToPreviousModsFolder();
                        }
                        catch (Exception e)
                        {
                            await DisplayErrors.DisplayGenericError("Failed to load mod folder", e);
                        }
                    },
                    "Loading previous mods folder")));
    }

    public bool CanRevertToPreviousModsFolder =>
        Directory.Exists(Path.Combine(_settings.ManagedFolder, PackManager.TempModStorage));
}