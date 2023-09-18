using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MsBox.Avalonia.Models;
using MsBox.Avalonia.Dto;
using Lumafly.Models;

namespace Lumafly.Util
{
    public static class PathUtil
    {
        // There isn't any [return: MaybeNullWhen(param is null)] so this overload will have to do
        // Not really a huge point but it's nice to have the nullable static analysis
        public static async Task<string?> SelectPathFallible() => await SelectPath(true);
        
        public static async Task<string> SelectPath(bool fail = false)
        {
            Debug.WriteLine("Selecting path...");

            Window parent = AvaloniaUtils.GetMainWindow();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return await SelectMacApp(parent, fail);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return await SelectExe(parent, fail);

            while (true)
            {
                var result = await parent.StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions()
                    {
                        AllowMultiple = false,
                        Title = Resources.PU_SelectPath
                    });
                if (result.Count == 0)
                {
                    await MessageBoxUtil
                        .GetMessageBoxStandardWindow(Resources.PU_InvalidPathTitle, Resources.PU_NoSelect).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());
                    if (fail) return null!;
                    continue;
                }
                
                if (ValidateWithSuffix(result[0].Path.LocalPath) is not var (managed, suffix))
                {
                    var res = await MessageBoxUtil.GetMessageBoxCustomWindow(new MessageBoxCustomParams {
                        ContentTitle = Resources.PU_InvalidPathTitle,
                        ContentHeader = Resources.PU_InvalidPathHeader,
                        ContentMessage = Resources.PU_InvalidPath,
                        MinHeight = 160,
                        ButtonDefinitions = FailedActionButtons
                    }).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());

                    if (res == Resources.XAML_AskForHelp) AskForHelp();
                    if (res == Resources.XAML_ReportError) ReportError();
                    
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
            // use old API because of inability to rigorously test
            #pragma warning disable CS0618
            
            var dialog = new OpenFileDialog
            {
                Title = Resources.PU_SelectApp,
                Directory = "/Applications",
                AllowMultiple = false
            };
            dialog.Filters?.Add(new FileDialogFilter { Extensions = { "app" } });
            
            #pragma warning restore CS0618

            while (true)
            {
                string[]? result = await dialog.ShowAsync(parent);
                if (result is null or { Length: 0 })
                {
                    await MessageBoxUtil
                        .GetMessageBoxStandardWindow(Resources.PU_InvalidPathTitle, Resources.PU_NoSelectMac).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());
                }
                else if (ValidateWithSuffix(result[0]) is not var (managed, suffix))
                {
                    var res = await MessageBoxUtil.GetMessageBoxCustomWindow(new MessageBoxCustomParams()
                    {
                        ContentTitle = Resources.PU_InvalidPathTitle,
                        ContentHeader = Resources.PU_InvalidAppHeader,
                        ContentMessage = Resources.PU_InvalidApp,
                        MinHeight = 200,
                        ButtonDefinitions = FailedActionButtons
                    }).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());

                    if (res == Resources.XAML_AskForHelp) AskForHelp();
                    if (res == Resources.XAML_ReportError) ReportError();
                }
                else
                    return Path.Combine(managed, suffix);

                if (fail)
                    return null!;
            }
        }
        
        private static async Task<string> SelectExe(Window parent, bool fail)
        {
            while (true)
            {
                var result = await parent.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
                {
                    Title = Resources.PU_SelectEXE,
                    AllowMultiple = true,
                    FileTypeFilter = new []
                    {
                        new FilePickerFileType("Game Executable")
                        {
                            Patterns = new []
                            {
                                "hollow_knight.exe", // steam 
                                "Hollow Knight.exe", // gog
                                "*.lnk"
                            }
                        },
                    }
                });
                
                if (result.Count == 0)
                {
                    await MessageBoxUtil.GetMessageBoxStandardWindow(Resources.PU_InvalidExeHeader, Resources.PU_NoSelect).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());
                    if (fail) return null!;
                    continue;
                }
                
                string? root = Path.GetDirectoryName(result[0].Path.LocalPath);
                if (root is null)
                {
                    await MessageBoxUtil.GetMessageBoxStandardWindow(Resources.PU_InvalidExeHeader, Resources.PU_NoSelect).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());
                    if (fail) return null!;
                    continue;
                }
                
                if (ValidateWithSuffix(root) is not var (managed, suffix))
                {
                    var res = await MessageBoxUtil.GetMessageBoxCustomWindow(new MessageBoxCustomParams {
                        ContentTitle = Resources.PU_InvalidPathTitle,
                        ContentHeader = Resources.PU_InvalidExeHeader,
                        ContentMessage = Resources.PU_InvalidExeHeader,
                        MinHeight = 160,
                        ButtonDefinitions = FailedActionButtons
                    }).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());

                    if (res == Resources.XAML_AskForHelp) AskForHelp();
                    if (res == Resources.XAML_ReportError) ReportError();
                    
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
        
        public static void AskForHelp() => Process.Start(new ProcessStartInfo("https://discord.gg/VDsg3HmWuB") { UseShellExecute = true });   
        public static void ReportError() => Process.Start(new ProcessStartInfo("https://github.com/TheMulhima/Lumafly/issues/new?assignees=&labels=bug&template=bug_report.yaml") { UseShellExecute = true });

    }
}
