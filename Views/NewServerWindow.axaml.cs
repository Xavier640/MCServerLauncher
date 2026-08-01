using Avalonia.Controls;
using Avalonia.Interactivity;
using MCServerLauncher.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using MCServerLauncher.Services;

namespace MCServerLauncher.Views;

public partial class NewServerWindow : Window
{
    public NewServerViewModel ViewModel { get; }

        public NewServerWindow()
    {
        InitializeComponent();
        ViewModel = new NewServerViewModel();
        DataContext = ViewModel;

        if (CreateButton != null)
            CreateButton.Click += OnCreateClick;

        if (CancelButton != null)
            CancelButton.Click += OnCancelClick;

        // Încarcă versiunile reale
        _ = LoadVersionsAsync();
    }

    private async Task LoadVersionsAsync()
    {
        try
        {
            var versions = await PaperDownloader.GetAvailableVersionsAsync();
            ViewModel.Versions.Clear();
            ViewModel.Versions.Add("latest");
            foreach (var v in versions.Take(30)) // primele 30
                ViewModel.Versions.Add(v);

            ViewModel.SelectedVersion = "latest";
        }
        catch
        {
            // rămân versiunile default din ViewModel
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