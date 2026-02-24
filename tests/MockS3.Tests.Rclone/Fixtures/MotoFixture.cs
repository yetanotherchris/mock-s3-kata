using System.Diagnostics;

namespace MockS3.Tests.Rclone.Fixtures;

public sealed class MotoFixture : IAsyncDisposable
{
    private string? _containerId;

    public bool IsAvailable { get; private set; }
    public string BaseUrl { get; private set; } = string.Empty;

    public MotoFixture()
    {
        Task.Run(StartCoreAsync).GetAwaiter().GetResult();
    }

    private async Task StartCoreAsync()
    {
        try
        {
            var versionResult = await RunDockerAsync(["--version"]);
            if (versionResult.ExitCode != 0) return;

            var runResult = await RunDockerAsync(
                ["run", "-d", "-p", "5000", "motoserver/moto:5.1.21"],
                TimeSpan.FromMinutes(3));
            if (runResult.ExitCode != 0 || string.IsNullOrWhiteSpace(runResult.Stdout)) return;

            _containerId = runResult.Stdout.Trim();

            await Task.Delay(500);

            var portResult = await RunDockerAsync(["port", _containerId, "5000"]);
            if (portResult.ExitCode != 0) return;

            var port = ParsePort(portResult.Stdout);
            if (port is null) return;

            BaseUrl = $"http://127.0.0.1:{port}";
            await WaitForReadyAsync();
            IsAvailable = true;
        }
        catch
        {
            // Docker unavailable or container failed to start — tests will skip
        }
    }

    private async Task WaitForReadyAsync()
    {
        using var http = new HttpClient();
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await http.GetAsync(BaseUrl + "/");
                return;
            }
            catch (HttpRequestException) { }
            await Task.Delay(200);
        }
    }

    private static string? ParsePort(string dockerPortOutput)
    {
        var line = dockerPortOutput.Trim().Split('\n')[0].Trim();
        var parts = line.Split(':');
        return parts.Length > 0 && int.TryParse(parts[^1], out var port) ? port.ToString() : null;
    }

    private static async Task<(int ExitCode, string Stdout)> RunDockerAsync(
        IEnumerable<string> args,
        TimeSpan? timeout = null)
    {
        try
        {
            var psi = new ProcessStartInfo("docker")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            foreach (var arg in args)
                psi.ArgumentList.Add(arg);

            using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start docker");
            var stdoutTask = process.StandardOutput.ReadToEndAsync();

            using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(30));
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                await stdoutTask;
                return (-1, string.Empty);
            }

            return (process.ExitCode, await stdoutTask);
        }
        catch
        {
            return (-1, string.Empty);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_containerId is not null)
        {
            await RunDockerAsync(["rm", "-f", _containerId]);
            _containerId = null;
        }
    }
}
