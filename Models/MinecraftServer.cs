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

    public string FolderPath { get; set; } = string.Empty;
}