using System.Text;
using System.Xml;
using System.Xml.Linq;
using MockS3.Errors;
using MockS3.Storage;

namespace MockS3.Routing;

// Minimal APIs are used rather than MVC because S3 routing doesn't map cleanly to MVC conventions.
// The same route can dispatch to different operations based on headers or query parameters:
// PUT /{bucket}/{key} → PutObject or CopyObject depending on x-amz-copy-source header
// GET /{bucket}       → ListObjectsV1 or ListObjectsV2 depending on list-type query parameter
public class S3RequestRouter
{
    private readonly InMemoryS3Storage _storage;

    public S3RequestRouter(InMemoryS3Storage storage)
    {
        _storage = storage;
    }

    public void Register(WebApplication app)
    {
        app.MapGet("/", HandleListBuckets);

        app.MapPut("/{bucket}", HandleCreateBucket);
        app.MapDelete("/{bucket}", HandleDeleteBucket);
        app.MapMethods("/{bucket}", ["HEAD"], HandleHeadBucket);
        app.MapGet("/{bucket}", HandleListObjects);
        app.MapPost("/{bucket}", HandlePostBucket);

        app.MapPut("/{bucket}/{**key}", HandlePutOrCopyObject);
        app.MapGet("/{bucket}/{**key}", HandleGetObject);
        app.MapDelete("/{bucket}/{**key}", HandleDeleteObject);
        app.MapMethods("/{bucket}/{**key}", ["HEAD"], HandleHeadObject);
    }

    public static string? GetNonEmptyQueryParam(IQueryCollection query, string name)
    {
        var value = query[name].ToString();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private IResult HandleListBuckets()
    {
        var buckets = _storage.ListBuckets().OrderBy(b => b.Name);
        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        }))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("ListAllMyBucketsResult");
            writer.WriteStartElement("Buckets");
            foreach (var b in buckets)
            {
                writer.WriteStartElement("Bucket");
                writer.WriteElementString("Name", b.Name);
                writer.WriteElementString("CreationDate", b.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
        return Results.Content(Encoding.UTF8.GetString(ms.ToArray()), "application/xml");
    }

    private IResult HandleCreateBucket(string bucket, HttpContext ctx)
    {
        _storage.GetOrCreateBucket(bucket);
        ctx.Response.Headers.Location = $"/{bucket}";
        return Results.StatusCode(200);
    }

    private IResult HandleDeleteBucket(string bucket)
    {
        if (!_storage.TryGetBucket(bucket, out var b))
            return S3ErrorResponse.NoSuchBucket($"/{bucket}").ToResult();

        if (b!.Objects.Count > 0)
            return S3ErrorResponse.BucketNotEmpty($"/{bucket}").ToResult();

        _storage.DeleteBucket(bucket);
        return Results.StatusCode(204);
    }

    private IResult HandleHeadBucket(string bucket)
    {
        if (!_storage.TryGetBucket(bucket, out _))
            return Results.StatusCode(404);

        return Results.StatusCode(200);
    }

    private IResult HandleListObjects(HttpContext ctx, string bucket)
    {
        if (!_storage.TryGetBucket(bucket, out var b))
            return S3ErrorResponse.NoSuchBucket($"/{bucket}").ToResult();

        var query = ctx.Request.Query;
        var isV2 = query["list-type"] == "2";
        var prefix = GetNonEmptyQueryParam(query, "prefix");
        var delimiter = GetNonEmptyQueryParam(query, "delimiter");
        var maxKeysStr = GetNonEmptyQueryParam(query, "max-keys");
        var maxKeys = maxKeysStr is not null && int.TryParse(maxKeysStr, out var mk) ? Math.Min(mk, 1000) : 1000;

        // Build unified listing: objects and common prefixes merged in sorted order
        var seenPrefixes = new HashSet<string>();
        var items = new List<(bool IsPrefix, string Key, S3Object? Obj)>();

        foreach (var obj in b!.Objects.Values.OrderBy(o => o.Key))
        {
            if (prefix is not null && !obj.Key.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            if (delimiter is not null)
            {
                var keyAfterPrefix = obj.Key[(prefix?.Length ?? 0)..];
                var delimIdx = keyAfterPrefix.IndexOf(delimiter, StringComparison.Ordinal);
                if (delimIdx >= 0)
                {
                    var commonPrefix = (prefix ?? "") + keyAfterPrefix[..(delimIdx + delimiter.Length)];
                    if (seenPrefixes.Add(commonPrefix))
                        items.Add((true, commonPrefix, null));
                    continue;
                }
            }

            items.Add((false, obj.Key, obj));
        }

        // Apply marker (V1) or continuation-token (V2)
        string? startAfter = null;
        if (isV2)
        {
            var token = GetNonEmptyQueryParam(query, "continuation-token");
            if (token is not null)
                startAfter = Encoding.UTF8.GetString(Convert.FromBase64String(token));
        }
        else
        {
            startAfter = GetNonEmptyQueryParam(query, "marker");
        }

        if (startAfter is not null)
            items = items.SkipWhile(item => string.CompareOrdinal(item.Key, startAfter) <= 0).ToList();

        var isTruncated = items.Count > maxKeys;
        var pageItems = items.Take(maxKeys).ToList();

        string? nextToken = null;
        if (isTruncated)
        {
            var lastKey = pageItems[^1].Key;
            nextToken = isV2
                ? Convert.ToBase64String(Encoding.UTF8.GetBytes(lastKey))
                : lastKey;
        }

        var pageContents = pageItems.Where(i => !i.IsPrefix).Select(i => i.Obj!).ToList();
        var pageCommonPrefixes = pageItems.Where(i => i.IsPrefix).Select(i => i.Key).ToList();

        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        }))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("ListBucketResult");
            writer.WriteElementString("Name", bucket);
            writer.WriteElementString("Prefix", prefix ?? "");
            writer.WriteElementString("MaxKeys", maxKeys.ToString());
            if (isV2)
                writer.WriteElementString("KeyCount", pageItems.Count.ToString());
            writer.WriteElementString("IsTruncated", isTruncated ? "true" : "false");
            if (nextToken is not null)
            {
                if (isV2)
                    writer.WriteElementString("NextContinuationToken", nextToken);
                else
                    writer.WriteElementString("NextMarker", nextToken);
            }

            foreach (var obj in pageContents)
            {
                writer.WriteStartElement("Contents");
                writer.WriteElementString("Key", obj.Key);
                writer.WriteElementString("LastModified", obj.LastModified.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                writer.WriteElementString("ETag", obj.ETag);
                writer.WriteElementString("Size", obj.ContentLength.ToString());
                writer.WriteEndElement();
            }

            foreach (var cp in pageCommonPrefixes)
            {
                writer.WriteStartElement("CommonPrefixes");
                writer.WriteElementString("Prefix", cp);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Results.Content(Encoding.UTF8.GetString(ms.ToArray()), "application/xml");
    }

    private async Task<IResult> HandlePostBucket(HttpContext ctx, string bucket)
    {
        if (!ctx.Request.Query.ContainsKey("delete"))
            return Results.StatusCode(405);

        if (!_storage.TryGetBucket(bucket, out var b))
            return S3ErrorResponse.NoSuchBucket($"/{bucket}").ToResult();

        var bodyBytes = await ReadBodyAsync(ctx);
        var doc = XDocument.Parse(Encoding.UTF8.GetString(bodyBytes));
        var keys = doc.Descendants().Where(e => e.Name.LocalName == "Key").Select(e => e.Value).ToList();

        foreach (var key in keys)
            b!.Objects.TryRemove(key, out _);

        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        }))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("DeleteResult");
            foreach (var key in keys)
            {
                writer.WriteStartElement("Deleted");
                writer.WriteElementString("Key", key);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Results.Content(Encoding.UTF8.GetString(ms.ToArray()), "application/xml");
    }

    private async Task<IResult> HandlePutOrCopyObject(HttpContext ctx, string bucket, string key)
    {
        if (ctx.Request.Headers.TryGetValue("x-amz-copy-source", out var copySource))
            return HandleCopyObject(bucket, key, copySource.ToString());

        return await HandlePutObject(ctx, bucket, key);
    }

    private async Task<IResult> HandlePutObject(HttpContext ctx, string bucket, string key)
    {
        if (!_storage.TryGetBucket(bucket, out _))
            return S3ErrorResponse.NoSuchBucket($"/{bucket}/{key}").ToResult();

        var content = await ReadBodyAsync(ctx);
        var contentType = ctx.Request.ContentType ?? "application/octet-stream";
        var userMetadata = ExtractUserMetadata(ctx.Request.Headers);

        var obj = _storage.PutObject(bucket, key, content, contentType, userMetadata);
        ctx.Response.Headers.ETag = obj.ETag;
        return Results.StatusCode(200);
    }

    private IResult HandleCopyObject(string destBucket, string destKey, string copySource)
    {
        var source = Uri.UnescapeDataString(copySource).TrimStart('/');
        var slashIdx = source.IndexOf('/');
        if (slashIdx < 0)
            return S3ErrorResponse.NoSuchKey(copySource).ToResult();

        var srcBucket = source[..slashIdx];
        var srcKey = source[(slashIdx + 1)..];

        if (!_storage.TryGetBucket(srcBucket, out var sb) || !sb!.Objects.TryGetValue(srcKey, out var srcObj))
            return S3ErrorResponse.NoSuchKey($"/{srcBucket}/{srcKey}").ToResult();

        if (!_storage.TryGetBucket(destBucket, out _))
            return S3ErrorResponse.NoSuchBucket($"/{destBucket}").ToResult();

        var newObj = _storage.PutObject(destBucket, destKey, srcObj.Content, srcObj.ContentType, srcObj.UserMetadata);

        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        }))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("CopyObjectResult");
            writer.WriteElementString("ETag", newObj.ETag);
            writer.WriteElementString("LastModified", newObj.LastModified.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Results.Content(Encoding.UTF8.GetString(ms.ToArray()), "application/xml");
    }

    private IResult HandleGetObject(HttpContext ctx, string bucket, string key)
    {
        if (!_storage.TryGetBucket(bucket, out var b))
            return S3ErrorResponse.NoSuchBucket($"/{bucket}/{key}").ToResult();

        if (!b!.Objects.TryGetValue(key, out var obj))
            return S3ErrorResponse.NoSuchKey($"/{bucket}/{key}").ToResult();

        ctx.Response.Headers.ETag = obj.ETag;
        ctx.Response.Headers.LastModified = obj.LastModified.ToString("R");
        foreach (var (k, v) in obj.UserMetadata)
            ctx.Response.Headers.Append($"x-amz-meta-{k}", v);
        return Results.Bytes(obj.Content, obj.ContentType);
    }

    private IResult HandleDeleteObject(string bucket, string key)
    {
        if (_storage.TryGetBucket(bucket, out var b))
            b!.Objects.TryRemove(key, out _);

        return Results.StatusCode(204);
    }

    private IResult HandleHeadObject(HttpContext ctx, string bucket, string key)
    {
        if (!_storage.TryGetBucket(bucket, out var b) || !b!.Objects.TryGetValue(key, out var obj))
            return Results.StatusCode(404);

        ctx.Response.Headers.ETag = obj.ETag;
        ctx.Response.Headers.LastModified = obj.LastModified.ToString("R");
        ctx.Response.Headers.ContentType = obj.ContentType;
        ctx.Response.Headers.ContentLength = obj.ContentLength;
        return Results.StatusCode(200);
    }

    private static async Task<byte[]> ReadBodyAsync(HttpContext ctx)
    {
        using var ms = new MemoryStream();
        await ctx.Request.Body.CopyToAsync(ms);
        return ms.ToArray();
    }

    private static Dictionary<string, string> ExtractUserMetadata(IHeaderDictionary headers)
    {
        const string metaPrefix = "x-amz-meta-";
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            if (header.Key.StartsWith(metaPrefix, StringComparison.OrdinalIgnoreCase))
                metadata[header.Key[metaPrefix.Length..]] = header.Value.ToString();
        }
        return metadata;
    }
}
