using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MCServerLauncher.Models;

namespace MCServerLauncher.Services;

public static class ServerListService
{
    private static string ListPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "MCServerLauncher",
        "servers.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static List<MinecraftServer> Load()
    {
        try
        {
            if (!File.Exists(ListPath))
                return new List<MinecraftServer>();

            var json = File.ReadAllText(ListPath);
            var list = JsonSerializer.Deserialize<List<MinecraftServer>>(json, JsonOptions);
            return list ?? new List<MinecraftServer>();
        }
        catch
        {
            return new List<MinecraftServer>();
        }
    }

    public static void Save(IEnumerable<MinecraftServer> servers)
    {
        var dir = Path.GetDirectoryName(ListPath)!;
        Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(servers, JsonOptions);
        File.WriteAllText(ListPath, json);
    }
}