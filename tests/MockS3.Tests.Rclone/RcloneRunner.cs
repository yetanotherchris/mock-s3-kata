using System.Diagnostics;

namespace MockS3.Tests.Rclone;

public sealed record RcloneResult(int ExitCode, string Stdout, string Stderr);

public static class RcloneRunner
{
    public static async Task<RcloneResult> RunAsync(
        IEnumerable<string> args,
        IReadOnlyDictionary<string, string>? env = null,
        TimeSpan? timeout = null)
    {
        var psi = new ProcessStartInfo("rclone")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        if (env is not null)
            foreach (var (k, v) in env)
                psi.Environment[k] = v;

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start rclone process");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            await Task.WhenAll(stdoutTask, stderrTask);
            throw new TimeoutException($"rclone timed out after {timeout ?? TimeSpan.FromSeconds(30)}");
        }

        return new RcloneResult(
            process.ExitCode,
            await stdoutTask,
            await stderrTask);
    }
}
