using System.Diagnostics;
using MockS3.Tests.Rclone.Fixtures;

namespace MockS3.Tests.Rclone;

[ClassDataSource<MotoFixture>(Shared = SharedType.PerClass)]
public class RcloneMotoTests(MotoFixture fixture)
{
    private static readonly bool RcloneAvailable = CheckRcloneOnPath();

    private static bool CheckRcloneOnPath()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("rclone")
            {
                ArgumentList = { "--version" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });
            process?.WaitForExit(3000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private void SkipIfUnavailable()
    {
        if (!RcloneAvailable) Skip.Test("rclone not found on PATH");
        if (!fixture.IsAvailable) Skip.Test("Docker unavailable or moto container failed to start");
    }

    private IReadOnlyDictionary<string, string> RcloneEnv() => new Dictionary<string, string>
    {
        ["RCLONE_CONFIG_MOTO_TYPE"] = "s3",
        ["RCLONE_CONFIG_MOTO_PROVIDER"] = "Other",
        ["RCLONE_CONFIG_MOTO_ACCESS_KEY_ID"] = "test",
        ["RCLONE_CONFIG_MOTO_SECRET_ACCESS_KEY"] = "test",
        ["RCLONE_CONFIG_MOTO_ENDPOINT"] = fixture.BaseUrl,
        ["RCLONE_CONFIG_MOTO_PATH_STYLE"] = "true",
        ["RCLONE_CONFIG_MOTO_REGION"] = "us-east-1"
    };

    private static string UniqueName() => Guid.NewGuid().ToString("N")[..16];

    [Test]
    public async Task Upload_ThenList_ShowsUploadedFile()
    {
        SkipIfUnavailable();

        var bucket = UniqueName();
        await RcloneRunner.RunAsync(["mkdir", $"moto:{bucket}"], RcloneEnv());

        var tmpFile = Path.Combine(Path.GetTempPath(), $"{UniqueName()}.txt");
        var fileName = Path.GetFileName(tmpFile);
        await File.WriteAllTextAsync(tmpFile, "hello from rclone");

        try
        {
            var uploadResult = await RcloneRunner.RunAsync(
                ["copy", tmpFile, $"moto:{bucket}"],
                RcloneEnv());

            await Assert.That(uploadResult.ExitCode).IsEqualTo(0);

            var listResult = await RcloneRunner.RunAsync(
                ["ls", $"moto:{bucket}"],
                RcloneEnv());

            await Assert.That(listResult.ExitCode).IsEqualTo(0);
            await Assert.That(listResult.Stdout.Contains(fileName)).IsTrue();
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Test]
    public async Task Upload_ThenDownload_RoundTrip()
    {
        SkipIfUnavailable();

        var bucket = UniqueName();
        await RcloneRunner.RunAsync(["mkdir", $"moto:{bucket}"], RcloneEnv());

        var tmpFile = Path.Combine(Path.GetTempPath(), $"{UniqueName()}.txt");
        var fileName = Path.GetFileName(tmpFile);
        var downloadDir = Path.Combine(Path.GetTempPath(), UniqueName());
        Directory.CreateDirectory(downloadDir);

        const string content = "round-trip test content";
        await File.WriteAllTextAsync(tmpFile, content);

        try
        {
            var uploadResult = await RcloneRunner.RunAsync(
                ["copy", tmpFile, $"moto:{bucket}"],
                RcloneEnv());
            await Assert.That(uploadResult.ExitCode).IsEqualTo(0);

            var downloadResult = await RcloneRunner.RunAsync(
                ["copy", $"moto:{bucket}/{fileName}", downloadDir],
                RcloneEnv());
            await Assert.That(downloadResult.ExitCode).IsEqualTo(0);

            var downloaded = await File.ReadAllTextAsync(Path.Combine(downloadDir, fileName));
            await Assert.That(downloaded).IsEqualTo(content);
        }
        finally
        {
            File.Delete(tmpFile);
            if (Directory.Exists(downloadDir))
                Directory.Delete(downloadDir, recursive: true);
        }
    }

    [Test]
    public async Task Upload_MultipleFiles_ListShowsAll()
    {
        SkipIfUnavailable();

        var bucket = UniqueName();
        await RcloneRunner.RunAsync(["mkdir", $"moto:{bucket}"], RcloneEnv());

        var tmpDir = Path.Combine(Path.GetTempPath(), UniqueName());
        Directory.CreateDirectory(tmpDir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tmpDir, "alpha.txt"), "a");
            await File.WriteAllTextAsync(Path.Combine(tmpDir, "beta.txt"), "b");

            var uploadResult = await RcloneRunner.RunAsync(
                ["copy", tmpDir, $"moto:{bucket}"],
                RcloneEnv());
            await Assert.That(uploadResult.ExitCode).IsEqualTo(0);

            var listResult = await RcloneRunner.RunAsync(
                ["ls", $"moto:{bucket}"],
                RcloneEnv());
            await Assert.That(listResult.ExitCode).IsEqualTo(0);
            await Assert.That(listResult.Stdout.Contains("alpha.txt")).IsTrue();
            await Assert.That(listResult.Stdout.Contains("beta.txt")).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tmpDir))
                Directory.Delete(tmpDir, recursive: true);
        }
    }
}
