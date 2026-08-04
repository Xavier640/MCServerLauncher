using Avalonia.Controls;
using Avalonia.Interactivity;
using MCServerLauncher.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using MCServerLauncher.Services;
using System;
using System.Collections.Generic;


namespace MCServerLauncher.Views;

public partial class NewServerWindow : Window
{
    public NewServerViewModel ViewModel { get; }

        public NewServerWindow()
    {
        InitializeComponent();
        ViewModel = new NewServerViewModel();
        DataContext = ViewModel;
        if (TypeCombo != null)
        {
            TypeCombo.SelectionChanged += async (_, _) =>
            {
                await LoadVersionsAsync();
            };
        }

        if (CreateButton != null)
            CreateButton.Click += OnCreateClick;

        if (CancelButton != null)
            CancelButton.Click += OnCancelClick;

            

        _ = LoadVersionsAsync();
    }

    private async Task LoadVersionsAsync()
{
    try
    {
        ViewModel.Versions.Clear();
        ViewModel.Versions.Add("latest");

        List<string> versions = ViewModel.SelectedType switch
        {
            "Vanilla" => await MojangVersionService.GetReleaseVersionsAsync(),
            "Fabric" => await ModLoaderInstaller.GetFabricGameVersionsAsync(),
            "Paper" => await PaperDownloader.GetAvailableVersionsAsync(),
            "Forge" => await ModLoaderInstaller.GetForgeMinecraftVersionsAsync(),
            "NeoForge" => await ModLoaderInstaller.GetNeoForgeVersionsAsync(),
            _ => await PaperDownloader.GetAvailableVersionsAsync()
        };

        foreach (var v in versions)
            ViewModel.Versions.Add(v);

        ViewModel.SelectedVersion = "latest";
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine(ex);
        Title = "ERROR loading versions: " + ex.Message;
    }
}


    private void OnCreateClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.Confirmed = true;
        Close(ViewModel);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.Confirmed = false;
        Close(null);
    }
}