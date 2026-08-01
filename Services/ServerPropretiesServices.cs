using System;
using System.Collections.Generic;
using System.IO;

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
        var path = Path.Combine(serverFolder, "server.properties");
        var lines = new List<string>();
        var propsCopy = new Dictionary<string, string>(properties, StringComparer.OrdinalIgnoreCase);

        // Păstrăm liniile existente și actualizăm valorile
        if (File.Exists(path))
        {
            foreach (var line in File.ReadAllLines(path))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                {
                    lines.Add(line);
                    continue;
                }

                var idx = trimmed.IndexOf('=');
                if (idx <= 0)
                {
                    lines.Add(line);
                    continue;
                }

                var key = trimmed[..idx].Trim();
                if (propsCopy.TryGetValue(key, out var newValue))
                {
                    lines.Add($"{key}={newValue}");
                    propsCopy.Remove(key);
                }
                else
                {
                    lines.Add(line);
                }
            }
        }

        // Adăugăm cheile noi (dacă există)
        foreach (var kv in propsCopy)
            lines.Add($"{kv.Key}={kv.Value}");

        File.WriteAllLines(path, lines);
    }
}