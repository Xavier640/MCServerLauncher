using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MCServerLauncher.Services;
using MCServerLauncher.ViewModels;
using Avalonia.Input.Platform;

namespace MCServerLauncher.Views;

public partial class PlayitWindow : Window
{
    private readonly PlayitViewModel _vm;
    private readonly PlayitService _playit;

    public PlayitWindow(PlayitService playit)
    {
        InitializeComponent();
        _playit = playit;
        _vm = new PlayitViewModel
        {
            SecretKey = playit.SecretKey ?? string.Empty,
            TunnelAddress = playit.TunnelAddress ?? string.Empty,
            Status = playit.IsRunning ? "Rulează" : "Oprit",
            IsRunning = playit.IsRunning
        };
        DataContext = _vm;

        _playit.LogReceived += line =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _vm.Log += line + Environment.NewLine;
                if (!string.IsNullOrEmpty(_playit.TunnelAddress))
                    _vm.TunnelAddress = _playit.TunnelAddress;
            });
        };

        _playit.Stopped += () =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _vm.Status = "Oprit";
                _vm.IsRunning = false;
            });
        };

        if (SaveKeyButton != null) SaveKeyButton.Click += OnSaveKey;
        if (OpenWebsiteButton != null) OpenWebsiteButton.Click += OnOpenWebsite;
        if (StartButton != null) StartButton.Click += OnStart;
        if (StopButton != null) StopButton.Click += OnStop;
        if (SaveAddressButton != null) SaveAddressButton.Click += OnSaveAddress;
        if (CopyButton != null) CopyButton.Click += OnCopy;
        if (CloseButton != null) CloseButton.Click += (_, _) => Close();
    }

    private void OnSaveKey(object? sender, RoutedEventArgs e)
    {
        _playit.SecretKey = _vm.SecretKey;
        _vm.Log += "Secret Key salvat.\n";
    }

    private void OnOpenWebsite(object? sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://playit.gg/account/agents",
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void OnStart(object? sender, RoutedEventArgs e)
    {
        try
        {
            _playit.SecretKey = _vm.SecretKey;
            _playit.Start();
            _vm.Status = "Rulează";
            _vm.IsRunning = true;
            _vm.Log += "Agent pornit.\n";
        }
        catch (Exception ex)
        {
            _vm.Log += "EROARE Start: " + ex.Message + "\n";
            _vm.Status = "Eroare";
        }
    }

    private void OnStop(object? sender, RoutedEventArgs e)
    {
        _playit.Stop();
        _vm.Status = "Oprit";
        _vm.IsRunning = false;
        _vm.Log += "Agent oprit.\n";
    }

    private void OnSaveAddress(object? sender, RoutedEventArgs e)
    {
        _playit.TunnelAddress = _vm.TunnelAddress;
        _vm.Log += "Adresa tunnel salvată.\n";
    }

    private async void OnCopy(object? sender, RoutedEventArgs e)
{
    if (string.IsNullOrWhiteSpace(_vm.TunnelAddress))
        return;

    try
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null)
        {
            _vm.Log += "Clipboard indisponibil.\n";
            return;
        }

        // Extension method din Avalonia.Input.Platform
        await clipboard.SetTextAsync(_vm.TunnelAddress);
        _vm.Log += "IP copiat în clipboard.\n";
    }
    catch (Exception ex)
    {
        _vm.Log += "Eroare copy: " + ex.Message + "\n";
    }
}
}