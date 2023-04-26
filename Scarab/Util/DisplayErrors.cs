using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MessageBox.Avalonia;
using MessageBox.Avalonia.Enums;
using Scarab.Models;
using Scarab.Services;

namespace Scarab.Util;

public static class DisplayErrors
{

    // for long running error loading tasks
    public static bool IsLoadingAnError { get; set; } = false;
    
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
        Trace.TraceError(e.ToString());

        await MessageBoxManager.GetMessageBoxStandardWindow
        (
            title: "Error!",
            text: $"An exception occured while {action} {name}.",
            icon: Icon.Error
        ).Show();
    }
    
    public static async Task DisplayGenericError(string errorText, Exception e)
    {
        Trace.TraceError(e.ToString());

        await MessageBoxManager.GetMessageBoxStandardWindow
        (
            title: "Error!",
            text: errorText,
            icon: Icon.Error
        ).Show();
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
}