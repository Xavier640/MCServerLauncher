using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

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
}