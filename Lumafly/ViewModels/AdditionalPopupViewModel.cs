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

public partial class AdditionalPopupViewModel : ViewModelBase
{
    public AdditionalPopupViewModel(string info)
    {
        Info = info;
    }
    
    [Notify] private string _info = "";

}
