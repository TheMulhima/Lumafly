using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using MsBox.Avalonia.Enums;
using PropertyChanged.SourceGenerator;
using Lumafly.Enums;
using Lumafly.Interfaces;
using Lumafly.Models;
using Lumafly.Services;
using Lumafly.Util;

namespace Lumafly.ViewModels;

public partial class InfoViewModel : ViewModelBase
{
    private readonly IInstaller _installer;
    private readonly IModSource _modSource;
    private readonly ISettings _settings;
    private readonly IUrlSchemeHandler _urlSchemeHandler;
    private readonly HttpClient _hc;
    
    [Notify]
    private bool _isLaunchingGame;
    [Notify]
    private string _additionalInfo = "";
    [Notify]
    private bool _additionalInfoVisible;
    
    public InfoViewModel(IInstaller installer, IModSource modSource ,ISettings settings, HttpClient hc, IUrlSchemeHandler urlSchemeHandler)
    {
        Trace.WriteLine("Initializing InfoViewModel");
        _installer = installer;
        _modSource = modSource;
        _settings = settings;
        _hc = hc;
        _urlSchemeHandler = urlSchemeHandler;
        Task.Run(FetchAdditionalInfo);
        Dispatcher.UIThread.Invoke(() => HandleLaunchUrlScheme(_urlSchemeHandler));
    }
    public void OpenLink(object link) => Process.Start(new ProcessStartInfo((string)link) { UseShellExecute = true });

    private const string hollow_knight = "hollow_knight";
    private const string HollowKnight = "Hollow Knight";

    public async Task LaunchGame(object _isVanilla) => await _LaunchGame(bool.Parse((string) _isVanilla));
    
    
    /// <summary>
    /// Launches the game
    /// </summary>
    /// <param name="isVanilla">Set to true for vanilla game, set to false for modded game and set to null for no change to current api state</param>
    private async Task _LaunchGame(bool? isVanilla)
    {
        Trace.WriteLine("Launching game");
        IsLaunchingGame = true;
        try
        {
            // remove any existing hk instance
            static bool IsHollowKnight(Process p) => (
                p.ProcessName.StartsWith(hollow_knight)
                || p.ProcessName.StartsWith(HollowKnight)
            );
            
            if (Process.GetProcesses().FirstOrDefault(IsHollowKnight) is { } proc) 
                proc.Kill();

            await _installer.CheckAPI();

            if (isVanilla != null)
            {
                if (!(_modSource.ApiInstall is NotInstalledState or InstalledState { Enabled: false } && isVanilla.Value
                      || _modSource.ApiInstall is InstalledState { Enabled: true } && !isVanilla.Value))
                {
                    await ModListViewModel.ToggleApiCommand(_modSource, _installer);
                }
            }

            var exeDetails = GetExecutableDetails();

            if (exeDetails.name is hollow_knight or hollow_knight + ".exe")
            {
                // assumption: hollow_knight is only used in steam. So might as well make steam run it
                Process.Start(new ProcessStartInfo("steam://rungameid/367520")
                {
                    UseShellExecute = true
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exeDetails.name,
                    WorkingDirectory = exeDetails.path,
                    UseShellExecute = true,
                });
            }

        }
        catch (Exception e)
        {
            await DisplayErrors.DisplayGenericError($"Unable to launch the game", e);
        }

        IsLaunchingGame = false;
    }

    private (string path, string name) GetExecutableDetails()
    {
        string exeName;
        
        // get exe path
        var managedFolder = new DirectoryInfo(_settings.ManagedFolder);
        var managedParent = managedFolder.Parent; // now in hollow_knight_data or (for mac) data folder
            
        var hkExeFolder = managedParent!.Parent; // now in the hk exe folder or (for mac) resources folder;
            
        // mac os path has 2 extra folders
        if (OperatingSystem.IsMacOS())
        {
            hkExeFolder = managedParent.Parent! // now in contents folder
                .Parent; // now in hk exe folder

            // an assumption that mac paths are always hollow_knight. probably wrong but idk what to do hopefully the 1 person who has 
            // Mac and is not on steam reports error ¯\_(ツ)_/¯
            exeName = hollow_knight;
        }
        else
        {
            exeName = managedParent.Name.Replace("_Data", string.Empty); //unity appends _Data to end of exe name
        }

        // cuz windows is non unix 
        if (OperatingSystem.IsWindows()) exeName += ".exe";

        if (hkExeFolder is null) throw new Exception("Hollow Knight executable not found");
        string exePath = hkExeFolder.FullName;

        return (exePath, exeName);
    }

    public async Task FetchAdditionalInfo()
    {
        const string additionalInfoLink = "https://raw.githubusercontent.com/TheMulhima/Lumafly/static-resources/AdditionalInfo.md";
        try
        {
            AdditionalInfo = await _hc.GetStringAsync2(
                _settings,
                new Uri(additionalInfoLink),
                new CancellationTokenSource(ModDatabase.TIMEOUT).Token);
            
            if (!string.IsNullOrEmpty(AdditionalInfo)) 
                AdditionalInfoVisible = true;
        }
        catch (Exception)
        {
            // ignored not important
        }
    }
    
    private async Task HandleLaunchUrlScheme(IUrlSchemeHandler urlSchemeHandler)
    {
        if (urlSchemeHandler is { Handled: false, UrlSchemeCommand: UrlSchemeCommands.launch })
        {
            if (urlSchemeHandler.Data is "")
                await _LaunchGame(null);
            else if (urlSchemeHandler.Data.ToLower() is "vanilla" or "false")
                await _LaunchGame(true);
            else if (urlSchemeHandler.Data.ToLower() is "modded" or "true")
                await _LaunchGame(false);
            else
                await _urlSchemeHandler.ShowConfirmation("Launch Game", 
                    "Launch game command is invalid. Please specify the launch as vanilla or modded or leave blank for regular launch", 
                    Icon.Warning);

            _urlSchemeHandler.FinishHandlingUrlScheme();
        }
    }
}