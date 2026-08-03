using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MCServerLauncher.ViewModels;

public partial class NewServerViewModel : ViewModelBase
{
    [ObservableProperty]
    private string serverName = "My Server";

    [ObservableProperty]
    private string selectedVersion = "latest";

    [ObservableProperty]
    private string selectedType = "Paper";

    public ObservableCollection<string> Versions { get; } = new()
    {
        "latest",
        "1.21.1",
        "1.20.4",
        "1.20.1",
        "1.19.4"
    };

public ObservableCollection<string> ServerTypes { get; } = new()
{
    "Paper",
    "Vanilla",
    "Fabric",
    "Forge",
    "NeoForge"
};

    public bool Confirmed { get; set; }
}