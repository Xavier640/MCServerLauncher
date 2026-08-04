using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCServerLauncher.Services;

public static class ModLoaderInstaller
{
    private static readonly HttpClient Http = new();

    public static async Task<string> InstallFabricAsync(string mcVersion, string targetFolder)
    {
        Directory.CreateDirectory(targetFolder);

        var loadersJson = await Http.GetStringAsync(
            $"https://meta.fabricmc.net/v2/versions/loader/{mcVersion}");
        using var loadersDoc = JsonDocument.Parse(loadersJson);
        var loaderVersion = loadersDoc.RootElement[0]
            .GetProperty("loader")
            .GetProperty("version")
            .GetString()!;

        var installersJson = await Http.GetStringAsync(
            "https://meta.fabricmc.net/v2/versions/installer");
        using var installersDoc = JsonDocument.Parse(installersJson);
        var installerVersion = installersDoc.RootElement[0]
            .GetProperty("version")
            .GetString()!;

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

    public static async Task<List<string>> GetForgeMinecraftVersionsAsync()
{
    var json = await Http.GetStringAsync(
        "https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json");
    using var doc = JsonDocument.Parse(json);

    var promos = doc.RootElement.GetProperty("promos");
    var mcVersions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var prop in promos.EnumerateObject())
    {
        var key = prop.Name;
        var idx = key.LastIndexOf('-');
        if (idx > 0)
            mcVersions.Add(key[..idx]);
    }

    return mcVersions
        .OrderByDescending(v => v, StringComparer.Ordinal)
        .ToList();
}

public static async Task<string> GetRecommendedForgeBuildAsync(string mcVersion)
{
    var json = await Http.GetStringAsync(
        "https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json");
    using var doc = JsonDocument.Parse(json);
    var promos = doc.RootElement.GetProperty("promos");

    if (promos.TryGetProperty($"{mcVersion}-recommended", out var rec))
        return rec.GetString()!;
    if (promos.TryGetProperty($"{mcVersion}-latest", out var lat))
        return lat.GetString()!;

    throw new Exception($"No Forge build found for Minecraft {mcVersion}");
}

public static async Task InstallForgeAsync(string mcVersion, string targetFolder)
{
    Directory.CreateDirectory(targetFolder);

    var forgeBuild = await GetRecommendedForgeBuildAsync(mcVersion);
    var full = $"{mcVersion}-{forgeBuild}";

    var url =
        $"https://maven.minecraftforge.net/net/minecraftforge/forge/{full}/forge-{full}-installer.jar";

    var installerPath = Path.Combine(targetFolder, "forge-installer.jar");
    var data = await Http.GetByteArrayAsync(url);
    await File.WriteAllBytesAsync(installerPath, data);

    await RunInstallerAsync(installerPath, targetFolder);

    await File.WriteAllTextAsync(Path.Combine(targetFolder, "eula.txt"), "eula=true\n");
    Directory.CreateDirectory(Path.Combine(targetFolder, "mods"));

    try { File.Delete(installerPath); } catch { }
}


public static async Task<List<string>> GetNeoForgeVersionsAsync()
{
    var json = await Http.GetStringAsync(
        "https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/neoforge");
    using var doc = JsonDocument.Parse(json);

    var versions = new List<string>();
    if (doc.RootElement.TryGetProperty("versions", out var arr))
    {
        foreach (var v in arr.EnumerateArray())
        {
            var s = v.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                versions.Add(s!);
        }
    }

    versions.Reverse();
    return versions;
}

public static async Task InstallNeoForgeAsync(string neoVersion, string targetFolder)
{
    Directory.CreateDirectory(targetFolder);

    var url =
        $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{neoVersion}/neoforge-{neoVersion}-installer.jar";

    var installerPath = Path.Combine(targetFolder, "neoforge-installer.jar");
    var data = await Http.GetByteArrayAsync(url);
    await File.WriteAllBytesAsync(installerPath, data);

    await RunInstallerAsync(installerPath, targetFolder);

    await File.WriteAllTextAsync(Path.Combine(targetFolder, "eula.txt"), "eula=true\n");
    Directory.CreateDirectory(Path.Combine(targetFolder, "mods"));

    try { File.Delete(installerPath); } catch { }
}


private static async Task RunInstallerAsync(string installerPath, string targetFolder)
{
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

    using var proc = Process.Start(psi)
        ?? throw new Exception("Could not start Java installer process.");

    var stdout = await proc.StandardOutput.ReadToEndAsync();
    var stderr = await proc.StandardError.ReadToEndAsync();
    await proc.WaitForExitAsync();

    if (proc.ExitCode != 0)
        throw new Exception($"Installer failed (exit {proc.ExitCode}): {stderr}\n{stdout}");
}
}