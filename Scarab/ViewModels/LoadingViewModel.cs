using System.Threading.Tasks;
using PropertyChanged.SourceGenerator;
using Scarab.Services;

namespace Scarab.ViewModels;

public partial class LoadingViewModel : ViewModelBase
{
    public LoadingViewModel(string? loadingText = null)
    {
        if (loadingText != null)
        {
            LoadingText = loadingText;
        }
        else
        {
            Task.Run(UpdateLoadingText);
        }
    }

    private static readonly string[] LoadingMessages = new[]
    {
        Resources.MVVM_Loading1,
        Resources.MVVM_Loading2,
        Resources.MVVM_Loading3,
        Resources.MVVM_Loading4,
        Resources.MVVM_Loading5,
        Resources.MVVM_Loading3,
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

    public async Task<bool> ShowUrlSchemePrompt(string prompt)
    {
        ShouldShowUrlSchemePrompt = true;
        UrlSchemePromptText = prompt;
        
        while (_shouldShowUrlSchemePrompt)
        {
            await Task.Delay(500);
        }

        return _accepted;
    }
    
    public void AcceptUrlSchemePrompt()
    {
        ShouldShowUrlSchemePrompt = false;
        _accepted = true;
    }
    
    public void DeclineUrlSchemePrompt()
    {
        ShouldShowUrlSchemePrompt = false;
        _accepted = false;
    }

    [Notify]
    private string _loadingText = "";

    [Notify] 
    private bool _shouldShowUrlSchemePrompt;

    [Notify]
    private string _urlSchemePromptText = "";

    private bool _accepted = false;

    public string PreUrlSchemePrompt => "Scarab has received the following command:";
    public string PostUrlSchemePrompt => "Do you want to run this command?";
}