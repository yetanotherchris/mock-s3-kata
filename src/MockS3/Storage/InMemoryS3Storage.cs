using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace MockS3.Storage;

public sealed class InMemoryS3Storage
{
    private readonly ConcurrentDictionary<string, S3Bucket> _buckets = new();

    public S3Bucket GetOrCreateBucket(string name) =>
        _buckets.GetOrAdd(name, static n => new S3Bucket(n));

    public bool TryGetBucket(string name, out S3Bucket? bucket) =>
        _buckets.TryGetValue(name, out bucket);

    public IEnumerable<S3Bucket> ListBuckets() => _buckets.Values;

    public bool DeleteBucket(string name) => _buckets.TryRemove(name, out _);

    public S3Object PutObject(
        string bucketName,
        string key,
        byte[] content,
        string contentType,
        IReadOnlyDictionary<string, string> userMetadata)
    {
        if (!_buckets.TryGetValue(bucketName, out var bucket))
            throw new InvalidOperationException($"Bucket '{bucketName}' does not exist.");

        var obj = new S3Object(
            Key: key,
            Content: content,
            ContentType: contentType,
            ETag: ComputeETag(content),
            LastModified: DateTimeOffset.UtcNow,
            UserMetadata: userMetadata);

        bucket.Objects[key] = obj;
        return obj;
    }

    public static string ComputeETag(byte[] content)
    {
        var hash = MD5.HashData(content);
        return $"\"{Convert.ToHexString(hash).ToLowerInvariant()}\"";
    }
}
