using System;
using Avalonia;
using System.Linq;

namespace MCServerLauncher;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Demo mode flag — MUST be inside Main
        App.IsDemoMode = args.Contains("--demo")
            || string.Equals(
                Environment.GetEnvironmentVariable("MCSL_DEMO"),
                "1",
                StringComparison.OrdinalIgnoreCase);

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}