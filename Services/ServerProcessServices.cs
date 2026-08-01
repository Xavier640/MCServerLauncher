using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MCServerLauncher.Services;

public class ServerProcessService
{
    private Process? _process;

    public bool IsRunning => _process != null && !_process.HasExited;

    public event Action<string>? OutputReceived;
    public event Action? ServerStopped;

    public void Start(string serverFolder, int maxRamGb = 2)
    {
        if (IsRunning)
            throw new InvalidOperationException("Serverul rulează deja.");

        var jarPath = Path.Combine(serverFolder, "server.jar");
        if (!File.Exists(jarPath))
            throw new FileNotFoundException("Nu am găsit server.jar", jarPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "java",
            Arguments = $"-Xms1G -Xmx{maxRamGb}G -jar server.jar nogui",
            WorkingDirectory = serverFolder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };

        _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        _process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                OutputReceived?.Invoke(e.Data);
        };

        _process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                OutputReceived?.Invoke(e.Data);
        };

        _process.Exited += (_, _) =>
        {
            ServerStopped?.Invoke();
            _process = null;
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    public async Task StopAsync()
    {
        var process = _process;
        if (process == null || process.HasExited)
            return;

        try
        {
            // Trimitem comanda stop în consolă
            await process.StandardInput.WriteLineAsync("stop");
            await process.StandardInput.FlushAsync();

            // Așteptăm maxim 15 secunde să se închidă frumos
            var exited = await Task.Run(() => process.WaitForExit(15000));
            if (!exited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            try { process?.Kill(entireProcessTree: true); } catch { }
        }
        finally
        {
            _process = null;
        }
    }
}