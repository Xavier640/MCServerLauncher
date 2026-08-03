using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MCServerLauncher.ViewModels;

public partial class ServerSettingsViewModel : ViewModelBase
{
    [ObservableProperty] private string motd = "A Minecraft Server";
    [ObservableProperty] private string maxPlayers = "20";
    [ObservableProperty] private bool onlineMode = true;
    [ObservableProperty] private bool whiteList = false;
    [ObservableProperty] private bool pvp = true;
    [ObservableProperty] private string difficulty = "easy";
    [ObservableProperty] private string gamemode = "survival";
    [ObservableProperty] private string serverPort = "25565";
    [ObservableProperty] private bool enableCommandBlock = false;
    [ObservableProperty] private string viewDistance = "10";
    [ObservableProperty] private string maxRamMb = "2048";

    public ObservableCollection<string> Difficulties { get; } = new()
    {
        "peaceful", "easy", "normal", "hard"
    };

    public ObservableCollection<string> Gamemodes { get; } = new()
    {
        "survival", "creative", "adventure", "spectator"
    };

    public bool Confirmed { get; set; }
}