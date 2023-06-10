using System.Collections.Generic;
using System.Threading.Tasks;
using MessageBox.Avalonia.DTO;
using Scarab.Enums;

namespace Scarab.Interfaces;

public interface IUrlSchemeHandler
{
    public void SetCommand(string arg);
    public Task ShowConfirmation(MessageBoxStandardParams param);
    public Dictionary<string, string?> ParseDownloadCommand(string data);
    
    public bool Handled {get; }
    public string Data {get; }
    public UrlSchemeCommands UrlSchemeCommand { get; }
}