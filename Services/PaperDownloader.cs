using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace MCServerLauncher.Services;

public static class PaperDownloader
{
    private static readonly HttpClient Http;

    static PaperDownloader()
    {
        Http = new HttpClient();
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("MCServerLauncher/1.0 ([email protected])");
    }

    public static async Task<(string JarPath, string Version)> DownloadLatestStableAsync(string targetFolder)
    {
        Directory.CreateDirectory(targetFolder);

        var projectJson = await Http.GetStringAsync("https://fill.papermc.io/v3/projects/paper");
        using var projectDoc = JsonDocument.Parse(projectJson);

        var versionsProp = projectDoc.RootElement.GetProperty("versions");
        var allVersions = versionsProp
            .EnumerateObject()
            .SelectMany(group => group.Value.EnumerateArray().Select(v => v.GetString()!))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct()
            .OrderByDescending(v => v, StringComparer.Ordinal)
            .ToList();

        if (allVersions.Count == 0)
            throw new Exception("Nu am gasit nicio versiune Paper.");

        string? chosenVersion = null;
        string? downloadUrl = null;

        foreach (var version in allVersions)
        {
            var buildsUrl = $"https://fill.papermc.io/v3/projects/paper/versions/{version}/builds";
            var buildsJson = await Http.GetStringAsync(buildsUrl);
            using var buildsDoc = JsonDocument.Parse(buildsJson);

            foreach (var build in buildsDoc.RootElement.EnumerateArray())
            {
                if (build.TryGetProperty("channel", out var channel) &&
                    channel.GetString() == "STABLE" &&
                    build.TryGetProperty("downloads", out var downloads) &&
                    downloads.TryGetProperty("server:default", out var serverDefault) &&
                    serverDefault.TryGetProperty("url", out var urlProp))
                {
                    chosenVersion = version;
                    downloadUrl = urlProp.GetString();
                    break;
                }
            }

            if (downloadUrl != null)
                break;
        }

        if (downloadUrl == null || chosenVersion == null)
            throw new Exception("Could not find a stable Paper build to download.");

        var jarPath = Path.Combine(targetFolder, "server.jar");
        var data = await Http.GetByteArrayAsync(downloadUrl);
        await File.WriteAllBytesAsync(jarPath, data);

        await File.WriteAllTextAsync(
            Path.Combine(targetFolder, "eula.txt"),
            "eula=true\n");

        return (jarPath, chosenVersion);
    }
        public static async Task<List<string>> GetAvailableVersionsAsync()
{
    var projectJson = await Http.GetStringAsync("https://fill.papermc.io/v3/projects/paper");
    using var projectDoc = JsonDocument.Parse(projectJson);

    var versionsProp = projectDoc.RootElement.GetProperty("versions");

    var allVersions = versionsProp
        .EnumerateObject()
        .SelectMany(group => group.Value.EnumerateArray().Select(v => v.GetString()!))
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Distinct()
        .ToList();

    // Sortare: încercăm să aliniem cu ordinea Mojang (releaseTime)
    try
    {
        var mojangOrder = await MojangVersionService.GetReleaseVersionsAsync();
        var orderIndex = mojangOrder
            .Select((id, i) => (id, i))
            .ToDictionary(x => x.id, x => x.i, StringComparer.OrdinalIgnoreCase);

        return allVersions
            .OrderBy(v => orderIndex.TryGetValue(v, out var idx) ? idx : int.MaxValue)
            .ThenByDescending(v => v, StringComparer.Ordinal)
            .ToList();
    }
    catch
    {
        // fallback: sortare text descrescătoare
        return allVersions
            .OrderByDescending(v => v, StringComparer.Ordinal)
            .ToList();
    }
}
    public static async Task<(string JarPath, string Version)> DownloadVersionAsync(string version, string targetFolder)
    {
        Directory.CreateDirectory(targetFolder);

        // Dacă e "latest", folosim metoda veche
        if (string.IsNullOrWhiteSpace(version) || version == "latest")
            return await DownloadLatestStableAsync(targetFolder);

        var buildsUrl = $"https://fill.papermc.io/v3/projects/paper/versions/{version}/builds";
        var buildsJson = await Http.GetStringAsync(buildsUrl);
        using var buildsDoc = JsonDocument.Parse(buildsJson);

        string? downloadUrl = null;

        foreach (var build in buildsDoc.RootElement.EnumerateArray())
        {
            if (build.TryGetProperty("channel", out var channel) &&
                channel.GetString() == "STABLE" &&
                build.TryGetProperty("downloads", out var downloads) &&
                downloads.TryGetProperty("server:default", out var serverDefault) &&
                serverDefault.TryGetProperty("url", out var urlProp))
            {
                downloadUrl = urlProp.GetString();
                break;
            }
        }

        // Dacă nu e STABLE, luăm primul build disponibil
        if (downloadUrl == null)
        {
            foreach (var build in buildsDoc.RootElement.EnumerateArray())
            {
                if (build.TryGetProperty("downloads", out var downloads) &&
                    downloads.TryGetProperty("server:default", out var serverDefault) &&
                    serverDefault.TryGetProperty("url", out var urlProp))
                {
                    downloadUrl = urlProp.GetString();
                    break;
                }
            }
        }

        if (downloadUrl == null)
            throw new Exception($"Nu am găsit download pentru versiunea {version}");

        var jarPath = Path.Combine(targetFolder, "server.jar");
        var data = await Http.GetByteArrayAsync(downloadUrl);
        await File.WriteAllBytesAsync(jarPath, data);

        await File.WriteAllTextAsync(
            Path.Combine(targetFolder, "eula.txt"),
            "eula=true\n");

        return (jarPath, version);
    }
}