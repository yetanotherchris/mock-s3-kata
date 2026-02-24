using System.Diagnostics;
using MockS3.Tests.Rclone.Fixtures;

namespace MockS3.Tests.Rclone;

[ClassDataSource<RcloneFixture>(Shared = SharedType.PerClass)]
public class RcloneTests(RcloneFixture fixture)
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

    private IReadOnlyDictionary<string, string> RcloneEnv() => new Dictionary<string, string>
    {
        ["RCLONE_CONFIG_MOCK_TYPE"] = "s3",
        ["RCLONE_CONFIG_MOCK_PROVIDER"] = "Other",
        ["RCLONE_CONFIG_MOCK_ACCESS_KEY_ID"] = "test",
        ["RCLONE_CONFIG_MOCK_SECRET_ACCESS_KEY"] = "test",
        ["RCLONE_CONFIG_MOCK_ENDPOINT"] = fixture.BaseUrl,
        ["RCLONE_CONFIG_MOCK_PATH_STYLE"] = "true"
    };

    private static string UniqueName() => Guid.NewGuid().ToString("N")[..16];

    [Test]
    public async Task Upload_ThenList_ShowsUploadedFile()
    {
        if (!RcloneAvailable) Skip.Test("rclone not found on PATH");

        var bucket = UniqueName();
        await fixture.Http.PutAsync($"/{bucket}", null);

        var tmpFile = Path.Combine(Path.GetTempPath(), $"{UniqueName()}.txt");
        var fileName = Path.GetFileName(tmpFile);
        await File.WriteAllTextAsync(tmpFile, "hello from rclone");

        try
        {
            var uploadResult = await RcloneRunner.RunAsync(
                ["copy", tmpFile, $"mock:{bucket}"],
                RcloneEnv());

            await Assert.That(uploadResult.ExitCode).IsEqualTo(0);

            var listResult = await RcloneRunner.RunAsync(
                ["ls", $"mock:{bucket}"],
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
        if (!RcloneAvailable) Skip.Test("rclone not found on PATH");

        var bucket = UniqueName();
        await fixture.Http.PutAsync($"/{bucket}", null);

        var tmpFile = Path.Combine(Path.GetTempPath(), $"{UniqueName()}.txt");
        var fileName = Path.GetFileName(tmpFile);
        var downloadDir = Path.Combine(Path.GetTempPath(), UniqueName());
        Directory.CreateDirectory(downloadDir);

        const string content = "round-trip test content";
        await File.WriteAllTextAsync(tmpFile, content);

        try
        {
            var uploadResult = await RcloneRunner.RunAsync(
                ["copy", tmpFile, $"mock:{bucket}"],
                RcloneEnv());
            await Assert.That(uploadResult.ExitCode).IsEqualTo(0);

            var downloadResult = await RcloneRunner.RunAsync(
                ["copy", $"mock:{bucket}/{fileName}", downloadDir],
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
        if (!RcloneAvailable) Skip.Test("rclone not found on PATH");

        var bucket = UniqueName();
        await fixture.Http.PutAsync($"/{bucket}", null);

        var tmpDir = Path.Combine(Path.GetTempPath(), UniqueName());
        Directory.CreateDirectory(tmpDir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tmpDir, "alpha.txt"), "a");
            await File.WriteAllTextAsync(Path.Combine(tmpDir, "beta.txt"), "b");

            var uploadResult = await RcloneRunner.RunAsync(
                ["copy", tmpDir, $"mock:{bucket}"],
                RcloneEnv());
            await Assert.That(uploadResult.ExitCode).IsEqualTo(0);

            var listResult = await RcloneRunner.RunAsync(
                ["ls", $"mock:{bucket}"],
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
