using CommunityToolkit.Mvvm.ComponentModel;

namespace MCServerLauncher.Models;

public partial class MinecraftServer : ObservableObject
{
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string version = "";
    [ObservableProperty] private string type = "";
    [ObservableProperty] private string status = "Stopped";
    [ObservableProperty] private string folderPath = "";
    [ObservableProperty] private int maxRamMb = 2048;
}