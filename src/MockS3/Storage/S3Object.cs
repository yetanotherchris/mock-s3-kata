namespace MockS3.Storage;

public sealed record S3Object(
    string Key,
    byte[] Content,
    string ContentType,
    string ETag,
    DateTimeOffset LastModified,
    IReadOnlyDictionary<string, string> UserMetadata)
{
    public long ContentLength => Content.LongLength;
}
