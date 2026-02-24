using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MockS3.Routing;
using MockS3.Storage;

namespace MockS3.Tests.Rclone.Fixtures;

public sealed class RcloneFixture : IAsyncDisposable
{
    private WebApplication? _app;
    public string BaseUrl { get; private set; } = string.Empty;
    public HttpClient Http { get; private set; } = null!;

    public RcloneFixture()
    {
        Task.Run(StartCoreAsync).GetAwaiter().GetResult();
    }

    private async Task StartCoreAsync()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton<InMemoryS3Storage>();
        builder.Services.AddSingleton<S3RequestRouter>();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _app = builder.Build();
        _app.Services.GetRequiredService<S3RequestRouter>().Register(_app);

        await _app.StartAsync();

        var addressesFeature = _app.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!;
        BaseUrl = addressesFeature.Addresses.First();
        Http = new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    public async ValueTask DisposeAsync()
    {
        Http?.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
