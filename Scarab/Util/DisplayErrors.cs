using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.Models;
using Scarab.CustomControls;
using Scarab.Models;
using Scarab.Services;
using Scarab.ViewModels;

namespace Scarab.Util;

public static class DisplayErrors
{
    public static async Task DisplayHashMismatch(HashMismatchException e)
    {
        await MessageBoxManager.GetMessageBoxStandardWindow
        (
            title: Resources.MLVM_DisplayHashMismatch_Msgbox_Title,
            text: string.Format(Resources.MLVM_DisplayHashMismatch_Msgbox_Text, e.Name, e.Actual, e.Expected),
            icon: Icon.Error
        ).Show();
    }

    public static async Task DisplayGenericError(string action, string name, Exception e)
    {
        await DisplayGenericError($"An exception occured while {action} {name}.", e);
    }
    
    public static async Task DisplayGenericError(string errorText, Exception? e = null)
    {
        if (e != null)
            Trace.TraceError(e.ToString());
        
        Window parent = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
                        ?? throw new InvalidOperationException();

        await new ErrorPopup()
        {
            DataContext = new ErrorPopupViewModel(errorText, e)
        }.ShowDialog(parent);
    }

    public static async Task DisplayNetworkError(string name, HttpRequestException e)
    {
        Trace.WriteLine($"Failed to download {name}, {e}");

        await MessageBoxManager.GetMessageBoxStandardWindow
        (
            title: Resources.MLVM_DisplayNetworkError_Msgbox_Title,
            text: string.Format(Resources.MLVM_DisplayNetworkError_Msgbox_Text, name),
            icon: Icon.Error
        ).Show();
    }
    
    // asks user for confirmation on whether or not they want to uninstall/disable mod.
    // returns whether or not user presses yes on the message box
    public static async Task<bool> DisplayHasDependentsWarning(string modName, IEnumerable<ModItem> dependents)
    {
        var dependentsString = string.Join(", ", dependents.Select(x => x.Name));
        var result = await MessageBoxManager.GetMessageBoxStandardWindow
        (
            title: "Warning! This mod is required for other mods to function!",
            text: $"{modName} is required for {dependentsString} to function properly. Do you still want to continue?",
            icon: Icon.Stop,
            @enum: ButtonEnum.YesNo
        ).Show();

        // return whether or not yes was clicked. Also don't remove mod when box is closed with the x
        return result.HasFlag(ButtonResult.Yes) && !result.HasFlag(ButtonResult.None);
    }
    
    public static async Task<bool> DisplayAreYouSureWarning(string warningText)
    {
        var result = await MessageBoxManager.GetMessageBoxStandardWindow
        (
            title: "Warning! Are you sure you want to do this?",
            text: warningText,
            icon: Icon.Stop,
            @enum: ButtonEnum.YesNo
        ).Show();

        // return whether or not yes was clicked. Also don't remove mod when box is closed with the x
        return result.HasFlag(ButtonResult.Yes) && !result.HasFlag(ButtonResult.None);
    }

    public static async Task DoActionAfterConfirmation(bool shouldAskForConfirmation, Func<Task<bool>> warningPopupDisplayer, Func<Task> action)
    {
        if (shouldAskForConfirmation)
        {
            if (await warningPopupDisplayer())
            {
                await action();
            }
        }
        else
        {
            await action();
        }
    }
    
    public static async Task HandleIOExceptionWhenDownloading(ModItem item, Exception e, string action)
    {
        string additionalText = "";
        string filePath = e.Message.Split('\'')[1];
        List<Process> processes = new List<Process>();
        string listOfProcesses = "";
        if (OperatingSystem.IsWindows())
        {
            try
            {
                processes = FileAccessLookup.WhoIsLocking(filePath);
                if (processes.Count > 0)
                {
                    listOfProcesses = $"{string.Join("\n-", processes.Select(x => x.ProcessName))}";
                    Trace.WriteLine($"Following processes is locking the file {listOfProcesses}");
                    additionalText =
                        $"\nPlease close the following processes as they are locking important files:\n{listOfProcesses}";
                }
            }
            catch (Exception)
            {
                //ignored as its not a requirement
            }
        }

        item.CallOnPropertyChanged(nameof(ModItem.InstallingButtonAccessible));

        await DisplayGenericError(
            $"Unable to {action} {item.Name}.\n" +
            $"Scarab was unable to access the file in the mods folder.\n" +
            $"Please make sure to close all other apps that could be using the mods folder\n" +
            additionalText, e);
    }
}