using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using PropertyChanged.SourceGenerator;
using ReactiveUI;

namespace Scarab.ViewModels;

public partial class ErrorPopupViewModel: ViewModelBase
{
    public ErrorPopupViewModel(string errorExplanation, Exception? e = null)
    {
        ErrorExplanation = errorExplanation;
        FullErrorText = $"Scarab Version: {Assembly.GetExecutingAssembly().GetName().Version}\n\n{e}";
        IsExpanderVisible = !string.IsNullOrEmpty(FullErrorText);
    }
    
    [Notify] private string _errorExplanation = "";
    [Notify] private string _fullErrorText = "";
    [Notify] private bool _isExpanderVisible = true;
    
    public static void AskForHelp() => Process.Start(new ProcessStartInfo("https://discord.gg/VDsg3HmWuB") { UseShellExecute = true });   
    public static void ReportError() => Process.Start(new ProcessStartInfo("https://github.com/TheMulhima/Scarab/issues/new?assignees=&labels=bug&template=bug_report.yaml") { UseShellExecute = true });

    public async Task Copy()
    {
        try
        {
            await Application.Current?.Clipboard?.SetTextAsync(ErrorExplanation + "\n\n" + FullErrorText)!;
        }
        catch (Exception)
        {
            // doesn't make sense so show error because the error message has errors
        }
    }
}
