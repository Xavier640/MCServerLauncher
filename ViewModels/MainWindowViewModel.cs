using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MCServerLauncher.Models;

namespace MCServerLauncher.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private MinecraftServer? _selectedServer;

    [ObservableProperty]
    private bool _canStart = true;

    [ObservableProperty]
    private bool _canStop = false;
}