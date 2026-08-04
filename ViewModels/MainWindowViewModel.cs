using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MCServerLauncher.Models;
using MCServerLauncher.Services;

namespace MCServerLauncher.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<MinecraftServer> Servers { get; } = new();

    [ObservableProperty]
    private MinecraftServer? selectedServer;

    [ObservableProperty]
    private bool isBusy;

    public ICommand CreateServerCommand { get; }
    public ICommand StartServerCommand { get; }
    public ICommand StopServerCommand { get; }
    public ICommand OpenFolderCommand { get; }

    public MainWindowViewModel()
    {
        ServerPathService.EnsureServersRootExists();

        CreateServerCommand = new AsyncRelayCommand(CreateServerAsync);
        StartServerCommand = new RelayCommand(StartServer);
        StopServerCommand = new RelayCommand(StopServer);
        OpenFolderCommand = new RelayCommand(OpenFolder);
       ServerPathService.EnsureServersRootExists();

    // Load saved servers
    foreach (var s in ServerListService.Load())
    {
        s.Status = "Stopped";
        Servers.Add(s);
    }
    }
    

    private async Task CreateServerAsync()
{
    if (IsBusy) return;

    try
    {
        IsBusy = true;

        var serverName = $"Server {Servers.Count + 1}";
        var folder = ServerPathService.GetServerFolder(serverName);

        var (jarPath, version) = await PaperDownloader.DownloadLatestStableAsync(folder);

        var server = new MinecraftServer
        {
            Name = serverName,
            Version = version,
            Type = "Paper",
            Status = "Stopped",
            FolderPath = folder
        };

        Servers.Add(server);
        SelectedServer = server;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine("EROARE: " + ex);
    }
    finally
    {
        IsBusy = false;
    }
}

    private void StartServer()
    {
        if (SelectedServer is null) return;
        SelectedServer.Status = "Running";
        Console.WriteLine("Start apăsat");
    }

    private void StopServer()
    {
        if (SelectedServer is null) return;
        SelectedServer.Status = "Stopped";
        Console.WriteLine("Stop apăsat");
    }

    private void OpenFolder()
    {
        if (SelectedServer is null || string.IsNullOrEmpty(SelectedServer.FolderPath))
            return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = SelectedServer.FolderPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("OpenFolder eroare: " + ex.Message);
        }
    }
}