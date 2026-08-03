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

    public void Start(string serverFolder, int maxRamMb = 2048)
{
    if (IsRunning)
        throw new InvalidOperationException("Serverul rulează deja.");

    if (maxRamMb < 512)
        maxRamMb = 2048;

    var jarPath = Path.Combine(serverFolder, "server.jar");
    var runBat = Path.Combine(serverFolder, "run.bat");
    var runSh = Path.Combine(serverFolder, "run.sh");

    ProcessStartInfo startInfo;

    if (File.Exists(jarPath))
    {
        startInfo = new ProcessStartInfo
        {
            FileName = "java",
            Arguments = $"-Xms512M -Xmx{maxRamMb}M -jar server.jar nogui",
            WorkingDirectory = serverFolder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };
    }
    else if (OperatingSystem.IsWindows() && File.Exists(runBat))
    {
        startInfo = new ProcessStartInfo
        {
            FileName = runBat,
            WorkingDirectory = serverFolder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };
    }
    else if (File.Exists(runSh))
    {
        startInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = "run.sh",
            WorkingDirectory = serverFolder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };
    }
    else
    {
        throw new FileNotFoundException(
            "Could not find server.jar, run.bat, or run.sh in the specified folder.");
    }

    _process = new Process
    {
        StartInfo = startInfo,
        EnableRaisingEvents = true
    };

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
        if (_process == null || _process.HasExited)
            return;

        try
        {
            await _process.StandardInput.WriteLineAsync("stop");
            await _process.StandardInput.FlushAsync();

            var exited = await Task.Run(() => _process.WaitForExit(15000));
            if (!exited)
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            try { _process.Kill(entireProcessTree: true); } catch { }
        }
        finally
        {
            _process = null;
        }
    }
    
}