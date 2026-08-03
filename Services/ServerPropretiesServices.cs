using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MCServerLauncher.Services;

public static class ServerPropertiesService
{
    public static Dictionary<string, string> Load(string serverFolder)
    {
        var path = Path.Combine(serverFolder, "server.properties");
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(path))
            return result;

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                continue;

            var idx = trimmed.IndexOf('=');
            if (idx <= 0) continue;

            var key = trimmed[..idx].Trim();
            var value = trimmed[(idx + 1)..].Trim();
            result[key] = value;
        }

        return result;
    }

    public static void Save(string serverFolder, Dictionary<string, string> properties)
{
    Directory.CreateDirectory(serverFolder);
    var path = Path.Combine(serverFolder, "server.properties");

    var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["motd"] = "A Minecraft Server",
        ["max-players"] = "20",
        ["online-mode"] = "true",
        ["white-list"] = "false",
        ["pvp"] = "true",
        ["difficulty"] = "easy",
        ["gamemode"] = "survival",
        ["server-port"] = "25565",
        ["enable-command-block"] = "false",
        ["view-distance"] = "10",
        ["spawn-protection"] = "16",
        ["level-name"] = "world"
    };

    foreach (var kv in properties)
        defaults[kv.Key] = kv.Value;

    if (!File.Exists(path))
    {
        var lines = defaults.Select(kv => $"{kv.Key}={kv.Value}").ToList();
        File.WriteAllLines(path, lines);
        return;
    }

    var existing = Load(serverFolder);
    foreach (var kv in defaults)
        existing[kv.Key] = kv.Value;

    var outLines = new List<string>();
    var written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var line in File.ReadAllLines(path))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
        {
            outLines.Add(line);
            continue;
        }

        var idx = trimmed.IndexOf('=');
        if (idx <= 0)
        {
            outLines.Add(line);
            continue;
        }

        var key = trimmed[..idx].Trim();
        if (existing.TryGetValue(key, out var val))
        {
            outLines.Add($"{key}={val}");
            written.Add(key);
        }
        else
        {
            outLines.Add(line);
        }
    }

    foreach (var kv in existing)
    {
        if (!written.Contains(kv.Key))
            outLines.Add($"{kv.Key}={kv.Value}");
    }

    File.WriteAllLines(path, outLines);
}
}