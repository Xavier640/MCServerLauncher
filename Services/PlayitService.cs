using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MCServerLauncher.Services;

public class PlayitService
{
    private Process? _process;

    public bool IsRunning => _process != null && !_process.HasExited;

    public string PlayitFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "MCServerLauncher",
        "playit");

    public string SecretKeyPath => Path.Combine(PlayitFolder, "secret.txt");
    public string TunnelAddressPath => Path.Combine(PlayitFolder, "tunnel_address.txt");

    public string? SecretKey
    {
        get => File.Exists(SecretKeyPath) ? File.ReadAllText(SecretKeyPath).Trim() : null;
        set
        {
            Directory.CreateDirectory(PlayitFolder);
            if (string.IsNullOrWhiteSpace(value))
            {
                if (File.Exists(SecretKeyPath)) File.Delete(SecretKeyPath);
            }
            else
            {
                File.WriteAllText(SecretKeyPath, value.Trim());
            }
        }
    }

    public string? TunnelAddress
    {
        get => File.Exists(TunnelAddressPath) ? File.ReadAllText(TunnelAddressPath).Trim() : null;
        set
        {
            Directory.CreateDirectory(PlayitFolder);
            if (string.IsNullOrWhiteSpace(value))
            {
                if (File.Exists(TunnelAddressPath)) File.Delete(TunnelAddressPath);
            }
            else
            {
                File.WriteAllText(TunnelAddressPath, value.Trim());
            }
        }
    }

    public event Action<string>? LogReceived;
    public event Action? Stopped;

    public void Start()
    {
        if (IsRunning)
            throw new InvalidOperationException("playit rulează deja.");

        if (string.IsNullOrWhiteSpace(SecretKey))
            throw new InvalidOperationException("Lipsește Secret Key. Configurează-l mai întâi.");

        Directory.CreateDirectory(PlayitFolder);

        var exePath = Path.Combine(PlayitFolder, "playit.exe");
        if (!File.Exists(exePath))
            exePath = "playit"; // fallback PATH / winget install

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"--secret {SecretKey}",
            WorkingDirectory = PlayitFolder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.Environment["SECRET_KEY"] = SecretKey;

        _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        _process.OutputDataReceived += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            LogReceived?.Invoke(e.Data);
            TryDetectTunnelAddress(e.Data);
        };

        _process.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            LogReceived?.Invoke(e.Data);
            TryDetectTunnelAddress(e.Data);
        };

        _process.Exited += (_, _) =>
        {
            Stopped?.Invoke();
            _process = null;
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    public void Stop()
    {
        if (_process == null || _process.HasExited) return;

        try
        {
            _process.Kill(entireProcessTree: true);
        }
        catch { }

        _process = null;
    }

    private void TryDetectTunnelAddress(string line)
    {
        var match = Regex.Match(line,
            @"([a-zA-Z0-9\-]+\.(?:gl\.)?(?:joinmc\.link|ply\.gg|playit\.gg)(?::\d+)?)");

        if (match.Success)
        {
            TunnelAddress = match.Groups[1].Value;
            LogReceived?.Invoke($"*** TUNNEL DETECTAT: {TunnelAddress} ***");
        }
    }
}