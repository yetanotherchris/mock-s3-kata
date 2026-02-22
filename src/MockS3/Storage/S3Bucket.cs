using System.Collections.Concurrent;

namespace MockS3.Storage;

public sealed class S3Bucket(string name)
{
    public string Name { get; } = name;
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    public ConcurrentDictionary<string, S3Object> Objects { get; } = new();
}
