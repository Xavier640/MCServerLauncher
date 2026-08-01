using Avalonia.Controls;
using Avalonia.Interactivity;
using MCServerLauncher.ViewModels;

namespace MCServerLauncher.Views;

public partial class ServerSettingsWindow : Window
{
    public ServerSettingsViewModel ViewModel { get; }

    public ServerSettingsWindow(ServerSettingsViewModel vm)
    {
        InitializeComponent();
        ViewModel = vm;
        DataContext = ViewModel;

        if (SaveButton != null)
            SaveButton.Click += (_, _) =>
            {
                ViewModel.Confirmed = true;
                Close(ViewModel);
            };

        if (CancelButton != null)
            CancelButton.Click += (_, _) =>
            {
                ViewModel.Confirmed = false;
                Close(null);
            };
    }
}