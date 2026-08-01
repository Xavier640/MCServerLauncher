using CommunityToolkit.Mvvm.ComponentModel;

namespace MCServerLauncher.ViewModels;

public partial class PlayitViewModel : ViewModelBase
{
    [ObservableProperty] private string secretKey = string.Empty;
    [ObservableProperty] private string tunnelAddress = string.Empty;
    [ObservableProperty] private string status = "Oprit";
    [ObservableProperty] private string log = string.Empty;
    [ObservableProperty] private bool isRunning;
}