using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MCServerLauncher.Services;

public static class ModLoaderInstaller
{
    private static readonly HttpClient Http = new();

    // ===================== FABRIC =====================

    public static async Task<string> InstallFabricAsync(string mcVersion, string targetFolder)
    {
        Directory.CreateDirectory(targetFolder);

        // loader versions
        var loadersJson = await Http.GetStringAsync(
            $"https://meta.fabricmc.net/v2/versions/loader/{mcVersion}");
        using var loadersDoc = JsonDocument.Parse(loadersJson);
        var loaderVersion = loadersDoc.RootElement[0]
            .GetProperty("loader")
            .GetProperty("version")
            .GetString()!;

        // installer versions
        var installersJson = await Http.GetStringAsync(
            "https://meta.fabricmc.net/v2/versions/installer");
        using var installersDoc = JsonDocument.Parse(installersJson);
        var installerVersion = installersDoc.RootElement[0]
            .GetProperty("version")
            .GetString()!;

        // server jar direct
        var url =
            $"https://meta.fabricmc.net/v2/versions/loader/{mcVersion}/{loaderVersion}/{installerVersion}/server/jar";

        var jarPath = Path.Combine(targetFolder, "server.jar");
        var data = await Http.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(jarPath, data);

        await File.WriteAllTextAsync(Path.Combine(targetFolder, "eula.txt"), "eula=true\n");
        Directory.CreateDirectory(Path.Combine(targetFolder, "mods"));

        return jarPath;
    }

    public static async Task<List<string>> GetFabricGameVersionsAsync()
    {
        var json = await Http.GetStringAsync("https://meta.fabricmc.net/v2/versions/game");
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement
            .EnumerateArray()
            .Where(x => x.TryGetProperty("stable", out var s) && s.GetBoolean())
            .Select(x => x.GetProperty("version").GetString()!)
            .ToList();
    }

    // ===================== NEOFORGE =====================

    public static async Task InstallNeoForgeAsync(string neoVersion, string targetFolder)
    {
        Directory.CreateDirectory(targetFolder);

        var url =
            $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{neoVersion}/neoforge-{neoVersion}-installer.jar";

        var installerPath = Path.Combine(targetFolder, "neoforge-installer.jar");
        var data = await Http.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(installerPath, data);

        // rulează installerul
        var psi = new ProcessStartInfo
        {
            FileName = "java",
            Arguments = $"-jar \"{installerPath}\" --installServer",
            WorkingDirectory = targetFolder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi)!;
        await proc.WaitForExitAsync();

        if (proc.ExitCode != 0)
            throw new Exception("NeoForge installer a eșuat.");

        await File.WriteAllTextAsync(Path.Combine(targetFolder, "eula.txt"), "eula=true\n");
        Directory.CreateDirectory(Path.Combine(targetFolder, "mods"));

        // șterge installerul
        try { File.Delete(installerPath); } catch { }
    }

    // ===================== FORGE =====================

    public static async Task InstallForgeAsync(string mcVersion, string forgeVersion, string targetFolder)
    {
        Directory.CreateDirectory(targetFolder);

        // format: 1.20.1-47.2.0
        var full = $"{mcVersion}-{forgeVersion}";
        var url =
            $"https://maven.minecraftforge.net/net/minecraftforge/forge/{full}/forge-{full}-installer.jar";

        var installerPath = Path.Combine(targetFolder, "forge-installer.jar");
        var data = await Http.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(installerPath, data);

        var psi = new ProcessStartInfo
        {
            FileName = "java",
            Arguments = $"-jar \"{installerPath}\" --installServer",
            WorkingDirectory = targetFolder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi)!;
        await proc.WaitForExitAsync();

        if (proc.ExitCode != 0)
            throw new Exception("Forge installer a eșuat.");

        await File.WriteAllTextAsync(Path.Combine(targetFolder, "eula.txt"), "eula=true\n");
        Directory.CreateDirectory(Path.Combine(targetFolder, "mods"));

        try { File.Delete(installerPath); } catch { }
    }
}