using CommunityToolkit.Mvvm.ComponentModel;

namespace MCServerLauncher.Models;

public partial class MinecraftServer : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _version = string.Empty;

    [ObservableProperty]
    private string _type = string.Empty;

    [ObservableProperty]
    private string _status = "Stopped";

    [ObservableProperty]
    private int _maxRamMb = 2048;

    public string FolderPath { get; set; } = string.Empty;
}