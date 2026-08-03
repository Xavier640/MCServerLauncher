using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace MCServerLauncher.Services;

public static class MojangVersionService
{
    private static readonly HttpClient Http = new();

    public static async Task<List<string>> GetReleaseVersionsAsync()
    {
        var json = await Http.GetStringAsync(
            "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json");

        using var doc = JsonDocument.Parse(json);
        var versions = doc.RootElement.GetProperty("versions");

        var list = new List<(string Id, DateTime ReleaseTime)>();

        foreach (var v in versions.EnumerateArray())
        {
            var type = v.GetProperty("type").GetString();
            if (type != "release")
                continue; // doar release-uri oficiale (nu snapshot / beta / alpha)

            var id = v.GetProperty("id").GetString()!;
            var releaseTime = DateTime.Parse(v.GetProperty("releaseTime").GetString()!);

            list.Add((id, releaseTime));
        }

        // Ordinea lansării: cele mai noi primele
        return list
            .OrderByDescending(x => x.ReleaseTime)
            .Select(x => x.Id)
            .ToList();
    }

    public static async Task<(string JarPath, string Version)> DownloadServerAsync(
    string version, string targetFolder)
{
    Directory.CreateDirectory(targetFolder);

    var manifestJson = await Http.GetStringAsync(
        "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json");
    using var manifest = JsonDocument.Parse(manifestJson);

    if (version == "latest")
        version = manifest.RootElement.GetProperty("latest").GetProperty("release").GetString()!;

    string? versionUrl = null;
    foreach (var v in manifest.RootElement.GetProperty("versions").EnumerateArray())
    {
        if (v.GetProperty("id").GetString() == version)
        {
            versionUrl = v.GetProperty("url").GetString();
            break;
        }
    }

    if (versionUrl == null)
        throw new Exception($"Version {version} was not found.");

    var versionJson = await Http.GetStringAsync(versionUrl);
    using var versionDoc = JsonDocument.Parse(versionJson);

    if (!versionDoc.RootElement.GetProperty("downloads").TryGetProperty("server", out var server))
        throw new Exception($"Version {version} has no server.jar (too old).");

    var jarUrl = server.GetProperty("url").GetString()!;
    var jarPath = Path.Combine(targetFolder, "server.jar");

    var data = await Http.GetByteArrayAsync(jarUrl);
    await File.WriteAllBytesAsync(jarPath, data);

    await File.WriteAllTextAsync(Path.Combine(targetFolder, "eula.txt"), "eula=true\n");

    return (jarPath, version);
}


}