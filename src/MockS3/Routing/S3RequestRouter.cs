using MockS3.Storage;

namespace MockS3.Routing;

// Minimal APIs are used rather than MVC because S3 routing doesn't map cleanly to MVC conventions.
// The same route can dispatch to different operations based on headers or query parameters:
// PUT /{bucket}/{key} → PutObject or CopyObject depending on x-amz-copy-source header
// GET /{bucket}       → ListObjectsV1 or ListObjectsV2 depending on list-type query parameter
public static class S3RequestRouter
{
    public static void Register(WebApplication app)
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

    private static IResult HandleListBuckets(InMemoryS3Storage storage)
        => Results.StatusCode(501);

    private static IResult HandleCreateBucket(string bucket, InMemoryS3Storage storage)
        => Results.StatusCode(501);

    private static IResult HandleDeleteBucket(string bucket, InMemoryS3Storage storage)
        => Results.StatusCode(501);

    private static IResult HandleHeadBucket(string bucket, InMemoryS3Storage storage)
        => Results.StatusCode(501);

    private static IResult HandleListObjects(HttpContext ctx, string bucket, InMemoryS3Storage storage)
        => Results.StatusCode(501);

    private static IResult HandlePostBucket(HttpContext ctx, string bucket, InMemoryS3Storage storage)
    {
        if (!ctx.Request.Query.ContainsKey("delete"))
            return Results.StatusCode(405);

        return Results.StatusCode(501);
    }

    private static IResult HandlePutOrCopyObject(HttpContext ctx, string bucket, string key, InMemoryS3Storage storage)
    {
        if (ctx.Request.Headers.ContainsKey("x-amz-copy-source"))
            return Results.StatusCode(501); // CopyObject

        return Results.StatusCode(501); // PutObject
    }

    private static IResult HandleGetObject(string bucket, string key, InMemoryS3Storage storage)
        => Results.StatusCode(501);

    private static IResult HandleDeleteObject(string bucket, string key, InMemoryS3Storage storage)
        => Results.StatusCode(501);

    private static IResult HandleHeadObject(string bucket, string key, InMemoryS3Storage storage)
        => Results.StatusCode(501);
}
