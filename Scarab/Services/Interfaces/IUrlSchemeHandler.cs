using System.Collections.Generic;
using System.Threading.Tasks;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using Scarab.Enums;

namespace Scarab.Interfaces;

public interface IUrlSchemeHandler
{
    public void SetCommand(string arg);
    public void SetCommand(UrlSchemeCommands arg);
    public Task ShowConfirmation(MessageBoxStandardParams param);
    public Task ShowConfirmation(string title, string message, Icon icon = Icon.Success);
    public Dictionary<string, string?> ParseDownloadCommand(string data);
    
    public bool Handled {get; }
    public string Data {get; }
    public UrlSchemeCommands UrlSchemeCommand { get; }
}