using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Models;
using Scarab.Models;
using Scarab.ViewModels;

namespace Scarab.Util
{
    public static class PathUtil
    {
        // There isn't any [return: MaybeNullWhen(param is null)] so this overload will have to do
        // Not really a huge point but it's nice to have the nullable static analysis
        public static async Task<string?> SelectPathFallible() => await SelectPath(true);
        
        public static async Task<string> SelectPath(bool fail = false)
        {
            Debug.WriteLine("Selecting path...");

            Window parent = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
                ?? throw new InvalidOperationException();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return await SelectMacApp(parent, fail);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return await SelectExe(parent, fail);
                
            var dialog = new OpenFolderDialog
            {
                Title = Resources.PU_SelectPath,
            };
            
            while (true)
            {
                string? result = await dialog.ShowAsync(parent);
                if (result is null)
                {
                    await MessageBoxUtil
                        .GetMessageBoxStandardWindow(Resources.PU_InvalidPathTitle, Resources.PU_NoSelect).Show();
                    if (fail) return null!;
                    continue;
                }
                
                if (ValidateWithSuffix(result) is not var (managed, suffix))
                {
                    var res = await MessageBoxUtil.GetMessageBoxCustomWindow(new MessageBoxCustomParams {
                        ContentTitle = Resources.PU_InvalidPathTitle,
                        ContentHeader = Resources.PU_InvalidPathHeader,
                        ContentMessage = Resources.PU_InvalidPath,
                        MinHeight = 160,
                        ButtonDefinitions = FailedActionButtons
                    }).Show();

                    if (res == Resources.XAML_AskForHelp) ErrorPopupViewModel.AskForHelp();
                    if (res == Resources.XAML_ReportError) ErrorPopupViewModel.ReportError();
                    
                }
                else
                {
                    return Path.Combine(managed, suffix);
                }

                if (fail) return null!;
            }
        }

        private static async Task<string> SelectMacApp(Window parent, bool fail)
        {
            var dialog = new OpenFileDialog
            {
                Title = Resources.PU_SelectApp,
                Directory = "/Applications",
                AllowMultiple = false
            };

            dialog.Filters?.Add(new FileDialogFilter { Extensions = { "app" } });

            while (true)
            {
                string[]? result = await dialog.ShowAsync(parent);

                if (result is null or { Length: 0 })
                    await MessageBoxUtil
                        .GetMessageBoxStandardWindow(Resources.PU_InvalidPathTitle, Resources.PU_NoSelectMac).Show();
                else if (ValidateWithSuffix(result.First()) is not (var managed, var suffix))
                {
                    var res = await MessageBoxUtil.GetMessageBoxCustomWindow(new MessageBoxCustomParams()
                    {
                        ContentTitle = Resources.PU_InvalidPathTitle,
                        ContentHeader = Resources.PU_InvalidAppHeader,
                        ContentMessage = Resources.PU_InvalidApp,
                        MinHeight = 200,
                        ButtonDefinitions = FailedActionButtons
                    }).Show();

                    if (res == Resources.XAML_AskForHelp) ErrorPopupViewModel.AskForHelp();
                    if (res == Resources.XAML_ReportError) ErrorPopupViewModel.ReportError();
                }
                else
                    return Path.Combine(managed, suffix);

                if (fail)
                    return null!;
            }
        }
        
        private static async Task<string> SelectExe(Window parent, bool fail)
        {
            var dialog = new OpenFileDialog
                {
                    Title = Resources.PU_SelectEXE,
                    Filters = new List<FileDialogFilter>
                    {
                        new ()
                        {
                            Extensions = new List<string> { "exe" }
                        }
                    },
                    AllowMultiple = false,
                };

            while (true)
            {
                string[]? result = await dialog.ShowAsync(parent);
                
                if (result?.FirstOrDefault() is null)
                {
                    await MessageBoxUtil
                        .GetMessageBoxStandardWindow(Resources.PU_InvalidPathTitle, Resources.PU_NoSelect).Show();
                    if (fail) return null!;
                    continue;
                }
                
                string? root = Path.GetDirectoryName(result.First());
                if (root is null)
                {
                    await MessageBoxUtil.GetMessageBoxStandardWindow(Resources.PU_InvalidPathTitle, Resources.PU_NoSelect).Show();
                    if (fail) return null!;
                    continue;
                }
                
                if (ValidateWithSuffix(root) is not var (managed, suffix))
                {
                    var res = await MessageBoxUtil.GetMessageBoxCustomWindow(new MessageBoxCustomParams {
                        ContentTitle = Resources.PU_InvalidPathTitle,
                        ContentHeader = Resources.PU_InvalidPathHeader,
                        ContentMessage = Resources.PU_InvalidPath,
                        MinHeight = 160,
                        ButtonDefinitions = FailedActionButtons
                    }).Show();

                    if (res == Resources.XAML_AskForHelp) ErrorPopupViewModel.AskForHelp();
                    if (res == Resources.XAML_ReportError) ErrorPopupViewModel.ReportError();
                    
                }
                else
                {
                    return Path.Combine(managed, suffix);
                }

                if (fail) return null!;
            }
        }

        private static ButtonDefinition[] FailedActionButtons => new ButtonDefinition[]
        {
            new() { Name = Resources.XAML_ReportError },
            new() { Name = Resources.XAML_AskForHelp },
            new()
            {
                IsCancel = true,
                IsDefault = true,
                Name = Resources.XAML_Ok
            },
        };

        private static readonly string[] SUFFIXES = new string[]
        {
            "Hollow Knight_Data", // GoG
            "hollow_knight_Data", // Steam
            Path.Combine("Contents", "Resources", "Data") // Mac
        };
        
        private const string MANAGED_FOLDER = "Managed";
        
        private static string[] MANAGED_SUFFIXES => SUFFIXES
            .Select(x => Path.Combine(x, MANAGED_FOLDER))
            .ToArray();

        public static ValidPath? ValidateWithSuffix(string root)
        {
            if (!Directory.Exists(root))
                return null;

            string? suffix = MANAGED_SUFFIXES.FirstOrDefault(s => Directory.Exists(Path.Combine(root, s)));

            if (suffix is null || !File.Exists(Path.Combine(root, suffix, "Assembly-CSharp.dll")))
                return null;

            return new ValidPath(root, suffix);
        }
        
        private static string RemoveIfEndsWith(this string @string, string toRemove)
        {
            if (@string.EndsWith(toRemove))
            {
                @string = @string[..^toRemove.Length];
                @string = @string.TrimEnd('/').TrimEnd('\\');
            }

            return @string;
        }

        public static bool ValidateExisting(string managed)
        {
            // We have the extra case of UnityEngine's dll here
            // because in cases with old directories or previous issues
            // the assembly dll can still exist, but UnityEngine.dll
            // is always unmodified, so we can rely on it.
            return Directory.Exists(managed)
                && File.Exists(Path.Combine(managed, "Assembly-CSharp.dll"))
                && File.Exists(Path.Combine(managed, "UnityEngine.dll"));
        }
    }
}
