using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Lumafly.ViewModels;

public partial class ImportPackPopupViewModel : ViewModelBase
{
    public readonly Regex ValidCodeRegex = new Regex("^[a-zA-Z0-9]{8}");

    public const string WebsiteShareLink = "https://themulhima.github.io/Lumafly/commands/modpack/?modpack=";
    
    private string _sharingCode = "";
    public string SharingCode
    {
        get => _sharingCode;
        set
        {
            _sharingCode = value;
            RaisePropertyChanged(nameof(SharingCode));
            RaisePropertyChanged(nameof(ValidSharingCode));

            if (!ValidSharingCode && !Debugger.IsAttached)
                throw new ArgumentException("Invalid sharing code. Must be 8 characters long.");
        }
    }

    public bool ValidSharingCode 
    {
        get
        {    
            if (_sharingCode.Contains(WebsiteShareLink))
                _sharingCode = _sharingCode.Replace(WebsiteShareLink, "");

            return ValidCodeRegex.IsMatch(_sharingCode);
        }
    }
}