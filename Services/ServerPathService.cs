using System;
using System.IO;

namespace MCServerLauncher.Services;

public static class ServerPathService
{
    public static string ServersRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "MCServerLauncher",
        "servers");

    public static string GetServerFolder(string serverName)
    {
        var safeName = string.Join("_", serverName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(ServersRoot, safeName);
    }

    public static void EnsureServersRootExists()
    {
        Directory.CreateDirectory(ServersRoot);
    }
}