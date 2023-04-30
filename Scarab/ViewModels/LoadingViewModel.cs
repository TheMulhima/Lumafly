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

    [Notify]
    private string _loadingText = "";
}