using System.Threading.Tasks;
using PropertyChanged.SourceGenerator;
using Scarab.Services;

namespace Scarab.ViewModels;

public partial class LoadingViewModel : ViewModelBase
{
    public LoadingViewModel()
    {
        Task.Run(UpdateLoadingText);
    }

    private static readonly string[] LoadingMessages = new[]
    {
        "Fetching mods from modlinks...",
        "Waiting for response from github...",
        "Still waiting...",
        "Trying from fallback source...",
        "Waiting for response from jsdelivr...",
        "Still waiting...",
    };

    private async Task UpdateLoadingText()
    {
        await Task.Delay(5000);
        foreach (var message in LoadingMessages)
        {
            LoadingText = message;
            await Task.Delay(ModDatabase.TIMEOUT/3);
        }
    }

    [Notify]
    private string _loadingText = "";
}