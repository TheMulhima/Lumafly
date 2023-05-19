using System.Threading.Tasks;
using MessageBox.Avalonia.DTO;

namespace Scarab.Interfaces;

public interface IUrlSchemeHandler
{
    public void Setup();
    public void SetCommand(string arg);
    public Task ShowConfirmation(MessageBoxStandardParams param);
    
    public bool Handled {get; }
    public string Data {get; }
    public UrlSchemeCommands UrlSchemeCommand { get; }
}