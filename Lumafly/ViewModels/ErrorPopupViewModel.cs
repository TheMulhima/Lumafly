using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using PropertyChanged.SourceGenerator;
using ReactiveUI;
using Lumafly.Util;

namespace Lumafly.ViewModels;

public partial class ErrorPopupViewModel: ViewModelBase
{
    public ErrorPopupViewModel(string errorExplanation, Exception? e = null)
    {
        ErrorExplanation = errorExplanation;
        FullErrorText = $"Lumafly Version: {Assembly.GetExecutingAssembly().GetName().Version}\n\n{e}";
        IsExpanderVisible = !string.IsNullOrEmpty(FullErrorText);
    }
    
    [Notify] private string _errorExplanation = "";
    [Notify] private string _fullErrorText = "";
    [Notify] private bool _isExpanderVisible = true;

    public void AskForHelp() => PathUtil.AskForHelp();
    public void ReportError() => PathUtil.ReportError();
}
