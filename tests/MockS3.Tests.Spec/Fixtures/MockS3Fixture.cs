using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MockS3.Storage;

namespace MockS3.Tests.Spec.Fixtures;

public sealed class MockS3Fixture : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public IAmazonS3 S3 { get; }
    public HttpClient Http { get; }
    public InMemoryS3Storage Storage => _factory.Services.GetRequiredService<InMemoryS3Storage>();

    public MockS3Fixture()
    {
        _factory = new WebApplicationFactory<Program>();
        Http = _factory.CreateDefaultClient(new UriNormalizingHandler());

        S3 = new AmazonS3Client(
            new BasicAWSCredentials("test", "test"),
            new AmazonS3Config
            {
                ServiceURL = "http://localhost",
                ForcePathStyle = true,
                HttpClientFactory = new SingletonHttpClientFactory(Http)
            });
    }

    public async ValueTask DisposeAsync()
    {
        S3.Dispose();
        await _factory.DisposeAsync();
    }

    private sealed class SingletonHttpClientFactory(HttpClient httpClient) : Amazon.Runtime.HttpClientFactory
    {
        public override HttpClient CreateHttpClient(IClientConfig clientConfig) => httpClient;
        public override bool DisposeHttpClientsAfterUse(IClientConfig clientConfig) => false;
    }

    private sealed class UriNormalizingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is not null)
                request.RequestUri = new Uri(request.RequestUri.OriginalString);

            if (request.Content is not null)
            {
                var sha256 = request.Headers.TryGetValues("x-amz-content-sha256", out var vals)
                    ? vals.FirstOrDefault()
                    : null;

                if (sha256?.StartsWith("STREAMING-") == true || request.Headers.Contains("x-amz-decoded-content-length"))
                {
                    var raw = await request.Content.ReadAsByteArrayAsync(cancellationToken);
                    var decoded = DecodeAwsChunked(raw);
                    var original = request.Content;
                    request.Content = new ByteArrayContent(decoded);
                    foreach (var header in original.Headers)
                        if (!request.Content.Headers.Contains(header.Key))
                            request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private static byte[] DecodeAwsChunked(byte[] data)
        {
            var result = new List<byte>();
            int pos = 0;
            while (pos < data.Length)
            {
                int lineEnd = -1;
                for (int i = pos; i < data.Length - 1; i++)
                {
                    if (data[i] == '\r' && data[i + 1] == '\n') { lineEnd = i; break; }
                }
                if (lineEnd == -1) break;

                var sizeLine = Encoding.ASCII.GetString(data, pos, lineEnd - pos);
                var semicolon = sizeLine.IndexOf(';');
                if (semicolon >= 0) sizeLine = sizeLine[..semicolon];

                if (!int.TryParse(sizeLine.Trim(), System.Globalization.NumberStyles.HexNumber, null, out var chunkSize))
                    break;

                pos = lineEnd + 2;
                if (chunkSize == 0) break;

                result.AddRange(data[pos..(pos + chunkSize)]);
                pos += chunkSize + 2;
            }
            return [.. result];
        }
    }
}
