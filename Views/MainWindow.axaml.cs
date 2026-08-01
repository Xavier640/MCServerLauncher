using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MCServerLauncher.Models;
using MCServerLauncher.Services;
using MCServerLauncher.ViewModels;
using System.Collections.Generic;

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
         if (SettingsButton != null)
            SettingsButton.Click += OnSettingsClick;

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
    var dialog = new NewServerWindow();
    var result = await dialog.ShowDialog<NewServerViewModel?>(this);

    if (result is null || !result.Confirmed)
        return;

    try
    {
        Title = "Se descarca serverul...";

        var serverName = string.IsNullOrWhiteSpace(result.ServerName)
            ? $"Server {_vm.Servers.Count + 1}"
            : result.ServerName;

        var folder = ServerPathService.GetServerFolder(serverName);

        // Deocamdată doar Paper (latest)
        // SelectedVersion o folosim mai târziu când adăugăm alegerea reală
        var (jarPath, version) = await PaperDownloader.DownloadVersionAsync(
        result.SelectedVersion,
        folder);

        var server = new MinecraftServer
        {
            Name = serverName,
            Version = version,
            Type = result.SelectedType,
            Status = "Stopped",
            FolderPath = folder
        };

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Servers.Add(server);
            _vm.SelectedServer = server;
        });

        Title = $"Server creat: {server.Type} {version}";
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
    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
{
    if (_vm.SelectedServer is null)
    {
        Title = "Selecteaza un server";
        return;
    }
    

    var props = ServerPropertiesService.Load(_vm.SelectedServer.FolderPath);

    var settingsVm = new ServerSettingsViewModel
    {
        Motd = props.GetValueOrDefault("motd", "A Minecraft Server"),
        MaxPlayers = props.GetValueOrDefault("max-players", "20"),
        OnlineMode = props.GetValueOrDefault("online-mode", "true") == "true",
        WhiteList = props.GetValueOrDefault("white-list", "false") == "true",
        Pvp = props.GetValueOrDefault("pvp", "true") == "true",
        Difficulty = props.GetValueOrDefault("difficulty", "easy"),
        Gamemode = props.GetValueOrDefault("gamemode", "survival"),
        ServerPort = props.GetValueOrDefault("server-port", "25565"),
        EnableCommandBlock = props.GetValueOrDefault("enable-command-block", "false") == "true",
        ViewDistance = props.GetValueOrDefault("view-distance", "10")
    };

    var dialog = new ServerSettingsWindow(settingsVm);
    var result = await dialog.ShowDialog<ServerSettingsViewModel?>(this);

    if (result is null || !result.Confirmed)
        return;

    var newProps = new Dictionary<string, string>
    {
        ["motd"] = result.Motd,
        ["max-players"] = result.MaxPlayers,
        ["online-mode"] = result.OnlineMode ? "true" : "false",
        ["white-list"] = result.WhiteList ? "true" : "false",
        ["pvp"] = result.Pvp ? "true" : "false",
        ["difficulty"] = result.Difficulty,
        ["gamemode"] = result.Gamemode,
        ["server-port"] = result.ServerPort,
        ["enable-command-block"] = result.EnableCommandBlock ? "true" : "false",
        ["view-distance"] = result.ViewDistance
    };

    ServerPropertiesService.Save(_vm.SelectedServer.FolderPath, newProps);
    Title = "Settings salvate";
}
    
}