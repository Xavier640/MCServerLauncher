using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MCServerLauncher.Models;
using MCServerLauncher.Services;
using MCServerLauncher.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;

namespace MCServerLauncher.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _vm;
    private readonly ServerProcessService _processService = new();
    private readonly PlayitService _playitService = new();   // ← AICI (lângă celelalte)

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnMainWindowClosing;

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

        if (ImportButton != null)
            ImportButton.Click += OnImportClick;

        if (DeleteButton != null)
            DeleteButton.Click += OnDeleteClick;

        if (PlayitButton != null)
        {
            PlayitButton.Click += async (_, _) =>
            {
                var win = new PlayitWindow(_playitService);
                await win.ShowDialog(this);
            };
        }

        _processService.ServerStopped += () =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_vm.SelectedServer != null)
                    _vm.SelectedServer.Status = "Stopped";
                Title = "Server stopped";
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
        Title = $"Installing {result.SelectedType}...";

        var serverName = string.IsNullOrWhiteSpace(result.ServerName)
            ? $"Server {_vm.Servers.Count + 1}"
            : result.ServerName.Trim();

        var folder = ServerPathService.GetServerFolder(serverName);
        var version = result.SelectedVersion;
        string finalVersion = version;

        switch (result.SelectedType)
        {
            case "Paper":
            {
                var paper = version == "latest"
                    ? await PaperDownloader.DownloadLatestStableAsync(folder)
                    : await PaperDownloader.DownloadVersionAsync(version, folder);
                finalVersion = paper.Version;
                break;
            }

            case "Fabric":
            {
                if (version == "latest")
                {
                    var list = await ModLoaderInstaller.GetFabricGameVersionsAsync();
                    if (list.Count == 0)
                        throw new Exception("No Fabric versions found.");
                    version = list[0];
                }
                await ModLoaderInstaller.InstallFabricAsync(version, folder);
                finalVersion = version;
                break;
            }

            case "Vanilla":
            {
                var vanilla = await MojangVersionService.DownloadServerAsync(version, folder);
                finalVersion = vanilla.Version;
                break;
            }

            case "Forge":
{
    if (version == "latest")
    {
        var list = await ModLoaderInstaller.GetForgeMinecraftVersionsAsync();
        if (list.Count == 0)
            throw new Exception("No Forge Minecraft versions found.");
        version = list[0];
    }
    await ModLoaderInstaller.InstallForgeAsync(version, folder);
    finalVersion = version;
    break;
}

        case "NeoForge":
        {
            if (version == "latest")
            {
                var list = await ModLoaderInstaller.GetNeoForgeVersionsAsync();
                if (list.Count == 0)
                    throw new Exception("No NeoForge versions found.");
                version = list[0];
            }
            await ModLoaderInstaller.InstallNeoForgeAsync(version, folder);
            finalVersion = version;
            break;
        }
        }

        var server = new MinecraftServer
        {
            Name = serverName,
            Version = finalVersion,
            Type = result.SelectedType,
            Status = "Stopped",
            FolderPath = folder,
            MaxRamMb = 2048
        };

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Servers.Add(server);
            _vm.SelectedServer = server;
            SaveServers();
        });
        SaveServers();
        Title = $"Created: {server.Type} {finalVersion}";
    }
    catch (Exception ex)
    {
        Title = "ERROR: " + ex.Message;
        System.Diagnostics.Debug.WriteLine(ex);
    }
}

    private async void OnStartClick(object? sender, RoutedEventArgs e)
{
    if (_vm.SelectedServer is null)
    {
        Title = "Select a server first";
        return;
    }

    try
    {
        // 1) Start playit agent (if secret key is configured)
        if (!string.IsNullOrWhiteSpace(_playitService.SecretKey))
        {
            if (!_playitService.IsRunning)
            {
                Title = "Starting playit agent...";
                _playitService.Start();
                // small delay so agent can connect
                await Task.Delay(2000);
            }
        }
        else
        {
            Title = "Warning: no playit Secret Key — local only";
        }

        // 2) Start Minecraft server
        var ramMb = _vm.SelectedServer.MaxRamMb;
        if (ramMb < 512) ramMb = 2048;

        _processService.Start(_vm.SelectedServer.FolderPath, ramMb);
        _vm.SelectedServer.Status = "Running";

        var tunnel = _playitService.TunnelAddress;
        if (!string.IsNullOrWhiteSpace(tunnel))
            Title = $"Server running | Public: {tunnel}";
        else if (!string.IsNullOrWhiteSpace(_playitService.SecretKey))
            Title = "Server + playit running (set tunnel address in playit.gg menu)";
        else
            Title = $"Server running ({ramMb} MB RAM) — localhost only";
    }
    catch (Exception ex)
    {
        Title = "ERROR Start: " + ex.Message;
    }
}

    private async void OnStopClick(object? sender, RoutedEventArgs e)
{
    if (_vm.SelectedServer is null) return;

    try
    {
        Title = "Stopping server...";
        await _processService.StopAsync();

        if (_vm.SelectedServer != null)
            _vm.SelectedServer.Status = "Stopped";

        _playitService.Stop();

        Title = "Server stopped";
    }
    catch (Exception ex)
    {
        Title = "ERROR Stop: " + ex.Message;
    }
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
            Title = "Error Opening Folder: " + ex.Message;
        }
    }
    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
{
    var propsPath = Path.Combine(_vm.SelectedServer.FolderPath, "server.properties");
    if (!File.Exists(propsPath))
    {
        Title = "server.properties missing — will be created on Save";
    }
    if (_vm.SelectedServer is null)
    {
        Title = "Select a server";
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
        ViewDistance = props.GetValueOrDefault("view-distance", "10"),
        MaxRamMb = _vm.SelectedServer.MaxRamMb.ToString()
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
        if (int.TryParse(result.MaxRamMb, out var ram) && ram >= 512)
    {
        _vm.SelectedServer.MaxRamMb = ram;
    }
    else
    {
        Title = "Invalid RAM value. Please enter a number greater than or equal to 512.";
        return;
    }

    ServerPropertiesService.Save(_vm.SelectedServer.FolderPath, newProps);
}
private async void OnImportClick(object? sender, RoutedEventArgs e)
{
    try
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = "Select the Minecraft server folder to import",
                AllowMultiple = false
            });

        if (folders.Count == 0)
            return;

        var folderPath = folders[0].Path.LocalPath;

        var hasJar = File.Exists(Path.Combine(folderPath, "server.jar"));
        var hasRunBat = File.Exists(Path.Combine(folderPath, "run.bat"));
        var hasRunSh = File.Exists(Path.Combine(folderPath, "run.sh"));
        var hasProperties = File.Exists(Path.Combine(folderPath, "server.properties"));

        if (!hasJar && !hasRunBat && !hasRunSh && !hasProperties)
        {
            Title = "Invalid folder: does not appear to be a Minecraft server";
            return;
        }

        var name = new DirectoryInfo(folderPath).Name;

        string type = "Unknown";
        if (Directory.Exists(Path.Combine(folderPath, "mods")))
        {
            if (File.Exists(Path.Combine(folderPath, "fabric-server-launch.jar")) ||
                Directory.Exists(Path.Combine(folderPath, ".fabric")))
                type = "Fabric";
            else if (Directory.Exists(Path.Combine(folderPath, "libraries")) && hasRunBat || hasRunSh)
                type = "Forge/NeoForge";
            else
                type = "Modded";
        }
        else if (hasJar)
        {
            type = "Paper/Vanilla";
        }

        if (_vm.Servers.Any(s =>
            string.Equals(s.FolderPath, folderPath, StringComparison.OrdinalIgnoreCase)))
        {
            Title = "Server already exists in the list.";
            return;
        }

        var server = new MinecraftServer
        {
            Name = name,
            Version = "?",
            Type = type,
            Status = "Stopped",
            FolderPath = folderPath
        };

        _vm.Servers.Add(server);
        _vm.SelectedServer = server;
        Title = $"Importat: {name}";
    }
    catch (Exception ex)
    {
        Title = "Import error: " + ex.Message;
    }
}
private void SaveServers()
{
    ServerListService.Save(_vm.Servers);
}
private async void OnDeleteClick(object? sender, RoutedEventArgs e)
{
    if (_vm.SelectedServer is null)
    {
        Title = "Select a server first";
        return;
    }

    var server = _vm.SelectedServer;

    // Optional: confirm dialog
    var confirm = new Window
    {
        Title = "Delete server?",
        Width = 360,
        Height = 160,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        CanResize = false
    };

    var yes = false;
    var panel = new StackPanel
    {
        Margin = new Thickness(20),
        Spacing = 12
    };
    panel.Children.Add(new TextBlock
    {
        Text = $"Delete \"{server.Name}\"?\nThis removes it from the list AND from the disk (if the folder exists).",
        TextWrapping = TextWrapping.Wrap
    });

    var buttons = new StackPanel
    {
        Orientation = Avalonia.Layout.Orientation.Horizontal,
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
        Spacing = 10
    };

    var cancelBtn = new Button { Content = "Cancel", Padding = new Thickness(12, 6) };
    var deleteBtn = new Button { Content = "Delete", Padding = new Thickness(12, 6) };

    cancelBtn.Click += (_, _) => confirm.Close();
    deleteBtn.Click += (_, _) =>
    {
        yes = true;
        confirm.Close();
    };

    buttons.Children.Add(cancelBtn);
    buttons.Children.Add(deleteBtn);
    panel.Children.Add(buttons);
    confirm.Content = panel;

    await confirm.ShowDialog(this);

    if (!yes) return;

    // Stop if running
    if (server.Status == "Running")
    {
        try { await _processService.StopAsync(); } catch { }
    }

    _vm.Servers.Remove(server);
    _vm.SelectedServer = null;
    SaveServers();
    Title = $"Deleted: {server.Name}";
    try
{
    if (Directory.Exists(server.FolderPath))
        Directory.Delete(server.FolderPath, recursive: true);
}
catch (Exception ex)
{
    Title = $"Removed from list, but folder delete failed: {ex.Message}";
}
}
private void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)
{
    // Stop server if running
    try
    {
        if (_processService.IsRunning)
            _ = _processService.StopAsync();
    }
    catch { }

    // Demo mode: delete temp files
    if (App.IsDemoMode)
    {
        try
        {
            var demoRoot = Path.Combine(Path.GetTempPath(), "MCServerLauncherDemo");
            if (Directory.Exists(demoRoot))
                Directory.Delete(demoRoot, recursive: true);
        }
        catch { }
    }
}

    
}