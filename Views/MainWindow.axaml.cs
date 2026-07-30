using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MCServerLauncher.Models;
using MCServerLauncher.Services;
using MCServerLauncher.ViewModels;

namespace MCServerLauncher.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _vm;
    private readonly ServerProcessService _processService = new();

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainWindowViewModel();
        DataContext = _vm;

        if (NewServerButton != null)
            NewServerButton.Click += OnNewServerClick;

        if (StartButton != null)
            StartButton.Click += OnStartClick;

        if (StopButton != null)
            StopButton.Click += OnStopClick;

        if (OpenFolderButton != null)
            OpenFolderButton.Click += OnOpenFolderClick;

        _processService.ServerStopped += () =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_vm.SelectedServer != null)
                    _vm.SelectedServer.Status = "Stopped";
                Title = "Server oprit";
            });
        };
    }

    private async void OnNewServerClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            Title = "Se descarca Paper...";

            var serverName = $"Server {_vm.Servers.Count + 1}";
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

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _vm.Servers.Add(server);
                _vm.SelectedServer = server;
            });

            Title = $"Server creat: Paper {version}";
        }
        catch (Exception ex)
        {
            Title = "EROARE: " + ex.Message;
        }
    }

    private void OnStartClick(object? sender, RoutedEventArgs e)
    {
        if (_vm.SelectedServer is null)
        {
            Title = "Selecteaza un server din lista";
            return;
        }

        try
        {
            _processService.Start(_vm.SelectedServer.FolderPath, maxRamGb: 2);
            _vm.SelectedServer.Status = "Running";
            Title = "Server pornit!";
        }
        catch (Exception ex)
        {
            Title = "EROARE Start: " + ex.Message;
        }
    }

    private async void OnStopClick(object? sender, RoutedEventArgs e)
    {
        if (_vm.SelectedServer is null) return;

        Title = "Se opreste serverul...";
        await _processService.StopAsync();
        _vm.SelectedServer.Status = "Stopped";
        Title = "Server oprit";
    }

    private void OnOpenFolderClick(object? sender, RoutedEventArgs e)
    {
        if (_vm.SelectedServer is null || string.IsNullOrEmpty(_vm.SelectedServer.FolderPath))
            return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _vm.SelectedServer.FolderPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Title = "EROARE Folder: " + ex.Message;
        }
    }
}