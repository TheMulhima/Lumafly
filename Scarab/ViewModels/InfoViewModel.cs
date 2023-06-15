using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PropertyChanged.SourceGenerator;
using Scarab.Interfaces;
using Scarab.Models;
using Scarab.Util;

namespace Scarab.ViewModels;

public partial class InfoViewModel : ViewModelBase
{
    private readonly IInstaller _installer;
    private readonly IModSource _modSource;
    private readonly ISettings _settings;
    
    [Notify]
    private bool _isLaunchingGame;
    
    public InfoViewModel(IInstaller installer, IModSource modSource ,ISettings settings)
    {
        _installer = installer;
        _modSource = modSource;
        _settings = settings;
    }
    public void OpenLink(object link) => Process.Start(new ProcessStartInfo((string)link) { UseShellExecute = true });

    private const string hollow_knight = "hollow_knight";
    private const string HollowKnight = "Hollow Knight";

    public async void LaunchGame(object _isVanilla)
    {
        IsLaunchingGame = true;
        try
        {
            var vanilla = bool.Parse((string)_isVanilla);
            
            // remove any existing hk instance
            static bool IsHollowKnight(Process p) => (
                p.ProcessName.StartsWith(hollow_knight)
                || p.ProcessName.StartsWith(HollowKnight)
            );
            
            if (Process.GetProcesses().FirstOrDefault(IsHollowKnight) is { } proc) 
                proc.Kill();

            await _installer.CheckAPI();

            if (!(_modSource.ApiInstall is NotInstalledState or InstalledState {Enabled: false} && vanilla
                  || _modSource.ApiInstall is InstalledState {Enabled: true} && !vanilla))
            {
                await ModListViewModel.ToggleApiCommand(_modSource, _installer);
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
}