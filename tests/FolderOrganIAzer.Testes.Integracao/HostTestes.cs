using Xunit;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Security.Principal;
using System;

namespace FolderOrganIAzer.Testes.Integracao;

public class HostTestes : IDisposable
{
    private readonly string _tempDir1;
    private readonly string _tempDir2;
    private Process? _hostProcess;

    public HostTestes()
    {
        _tempDir1 = Path.Combine(Path.GetTempPath(), "FolderOrganIAzer_HostTestes", Guid.NewGuid().ToString());
        _tempDir2 = Path.Combine(Path.GetTempPath(), "FolderOrganIAzer_HostTestes", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir1);
        Directory.CreateDirectory(_tempDir2);
    }

    public void Dispose()
    {
        if (_hostProcess != null && !_hostProcess.HasExited)
        {
            _hostProcess.Kill();
            _hostProcess.WaitForExit();
        }

        try { Directory.Delete(_tempDir1, true); } catch { }
        try { Directory.Delete(_tempDir2, true); } catch { }
    }

    private string GetHostExePath()
    {
        var basePath = AppContext.BaseDirectory;
        var config = basePath.Contains("Release") ? "Release" : "Debug";
        var exePath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "..", "src", "FolderOrganIAzer.Host", "bin", config, "net10.0-windows10.0.19041.0", "FolderOrganIAzer.Host.exe"));
        
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException(exePath);
        }
        return exePath;
    }

    [Fact]
    public async Task Host_RespondeSaudePipe()
    {
        var exePath = GetHostExePath();

        var psi = new ProcessStartInfo(exePath, $"--pasta-dados \"{_tempDir1}\"")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        _hostProcess = Process.Start(psi);
        Assert.NotNull(_hostProcess);

        await Task.Delay(3000);

        var sid = WindowsIdentity.GetCurrent().User?.Value;
        var pipeName = $"FolderOrganIAzer-{sid}";

        var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (context, token) =>
            {
                var stream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                await stream.ConnectAsync(token);
                return stream;
            }
        };

        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var response = await client.GetStringAsync("/v1/saude");

        Assert.Contains("\"versaoApp\"", response);
        Assert.Contains("\"versaoEsquema\"", response);
        Assert.Contains("\"bancoIntegro\"", response);
    }

    [Fact]
    public async Task Host_SegundaInstancia_FalhaSemCriarBanco()
    {
        var exePath = GetHostExePath();

        var psi = new ProcessStartInfo(exePath, $"--pasta-dados \"{_tempDir1}\"")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        _hostProcess = Process.Start(psi);
        Assert.NotNull(_hostProcess);

        await Task.Delay(3000);

        var psi2 = new ProcessStartInfo(exePath, $"--pasta-dados \"{_tempDir2}\"")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        var secondProcess = Process.Start(psi2);
        Assert.NotNull(secondProcess);
        
        secondProcess.WaitForExit(5000);
        Assert.True(secondProcess.HasExited);
        Assert.NotEqual(0, secondProcess.ExitCode);

        var arquivosDb = Directory.GetFiles(_tempDir2, "*.db");
        Assert.Empty(arquivosDb);
    }
}
