using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using MockS3.Tests.Spec.Fixtures;

namespace MockS3.Tests.Spec.Objects;

[ClassDataSource<MockS3Fixture>(Shared = SharedType.PerClass)]
public class ObjectTests(MockS3Fixture fixture)
{
    private static string UniqueName() => Guid.NewGuid().ToString("N")[..16];

    [Test]
    public async Task PutObject_ExistingBucket_Returns200WithETag()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });

        var response = await fixture.S3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = "test.txt",
            ContentBody = "hello"
        });

        await Assert.That(response.HttpStatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.ETag).IsNotNull();
    }

    [Test]
    public async Task PutObject_NonExistentBucket_ThrowsNoSuchBucket()
    {
        AmazonS3Exception? ex = null;
        try
        {
            await fixture.S3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = UniqueName(),
                Key = "key",
                ContentBody = "hello"
            });
        }
        catch (AmazonS3Exception e) { ex = e; }

        await Assert.That(ex).IsNotNull();
        await Assert.That(ex!.ErrorCode).IsEqualTo("NoSuchBucket");
    }

    [Test]
    public async Task PutObject_StoresContentType()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        await fixture.S3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = "test.txt",
            ContentBody = "hello",
            ContentType = "text/plain"
        });

        var response = await fixture.S3.GetObjectAsync(new GetObjectRequest { BucketName = bucket, Key = "test.txt" });

        await Assert.That(response.Headers.ContentType?.StartsWith("text/plain") ?? false).IsTrue();
    }

    [Test]
    public async Task PutObject_StoresUserMetadata()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        await fixture.S3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = "test.txt",
            ContentBody = "hello",
            Metadata = { ["author"] = "alice" }
        });

        var response = await fixture.S3.GetObjectAsync(new GetObjectRequest { BucketName = bucket, Key = "test.txt" });

        await Assert.That(response.Metadata["x-amz-meta-author"]).IsEqualTo("alice");
    }

    [Test]
    public async Task GetObject_ReturnsStoredContent()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });

        await fixture.S3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = "test.txt",
            ContentBody = "hello world"
        });

        var response = await fixture.S3.GetObjectAsync(new GetObjectRequest { BucketName = bucket, Key = "test.txt" });
        using var reader = new StreamReader(response.ResponseStream);
        var body = await reader.ReadToEndAsync();

        await Assert.That(response.HttpStatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body).IsEqualTo("hello world");
    }

    [Test]
    public async Task GetObject_ETagMatchesPutObject()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });

        var putResponse = await fixture.S3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = "test.txt",
            ContentBody = "hello"
        });
        var getResponse = await fixture.S3.GetObjectAsync(new GetObjectRequest { BucketName = bucket, Key = "test.txt" });

        await Assert.That(getResponse.ETag).IsEqualTo(putResponse.ETag);
    }

    [Test]
    public async Task GetObject_NonExistentKey_ThrowsNoSuchKey()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });

        AmazonS3Exception? ex = null;
        try { await fixture.S3.GetObjectAsync(new GetObjectRequest { BucketName = bucket, Key = "missing" }); }
        catch (AmazonS3Exception e) { ex = e; }

        await Assert.That(ex).IsNotNull();
        await Assert.That(ex!.ErrorCode).IsEqualTo("NoSuchKey");
    }

    [Test]
    public async Task GetObject_NonExistentBucket_ThrowsNoSuchBucket()
    {
        AmazonS3Exception? ex = null;
        try { await fixture.S3.GetObjectAsync(new GetObjectRequest { BucketName = UniqueName(), Key = "key" }); }
        catch (AmazonS3Exception e) { ex = e; }

        await Assert.That(ex).IsNotNull();
        await Assert.That(ex!.ErrorCode).IsEqualTo("NoSuchBucket");
    }

    [Test]
    public async Task HeadObject_ExistingObject_Returns200WithHeaders()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        await fixture.S3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = "test.txt",
            ContentBody = "hello",
            ContentType = "text/plain"
        });

        var response = await fixture.S3.GetObjectMetadataAsync(new GetObjectMetadataRequest { BucketName = bucket, Key = "test.txt" });

        await Assert.That(response.HttpStatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.ETag).IsNotNull();
        await Assert.That(response.ContentLength).IsGreaterThan(0L);
    }

    [Test]
    public async Task HeadObject_NonExistentObject_Returns404()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });

        AmazonS3Exception? ex = null;
        try { await fixture.S3.GetObjectMetadataAsync(new GetObjectMetadataRequest { BucketName = bucket, Key = "missing" }); }
        catch (AmazonS3Exception e) { ex = e; }

        await Assert.That(ex).IsNotNull();
        await Assert.That(ex!.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteObject_ExistingObject_Returns204()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        await fixture.S3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = "test.txt", ContentBody = "hello" });

        var response = await fixture.Http.DeleteAsync($"/{bucket}/test.txt");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeleteObject_NonExistentObject_Returns204Idempotent()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });

        var response = await fixture.Http.DeleteAsync($"/{bucket}/missing.txt");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task CopyObject_ExistingSource_Returns200WithETag()
    {
        var srcBucket = UniqueName();
        var destBucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = srcBucket });
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = destBucket });
        await fixture.S3.PutObjectAsync(new PutObjectRequest { BucketName = srcBucket, Key = "src.txt", ContentBody = "hello" });

        var response = await fixture.S3.CopyObjectAsync(new CopyObjectRequest
        {
            SourceBucket = srcBucket,
            SourceKey = "src.txt",
            DestinationBucket = destBucket,
            DestinationKey = "dest.txt"
        });

        await Assert.That(response.HttpStatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.ETag).IsNotNull();
    }

    [Test]
    public async Task CopyObject_CopiesContentToDestination()
    {
        var srcBucket = UniqueName();
        var destBucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = srcBucket });
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = destBucket });
        await fixture.S3.PutObjectAsync(new PutObjectRequest { BucketName = srcBucket, Key = "src.txt", ContentBody = "hello" });

        await fixture.S3.CopyObjectAsync(new CopyObjectRequest
        {
            SourceBucket = srcBucket,
            SourceKey = "src.txt",
            DestinationBucket = destBucket,
            DestinationKey = "dest.txt"
        });

        var getResponse = await fixture.S3.GetObjectAsync(new GetObjectRequest { BucketName = destBucket, Key = "dest.txt" });
        using var reader = new StreamReader(getResponse.ResponseStream);
        var body = await reader.ReadToEndAsync();

        await Assert.That(body).IsEqualTo("hello");
    }

    [Test]
    public async Task CopyObject_NonExistentSource_ThrowsNoSuchKey()
    {
        var srcBucket = UniqueName();
        var destBucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = srcBucket });
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = destBucket });

        AmazonS3Exception? ex = null;
        try
        {
            await fixture.S3.CopyObjectAsync(new CopyObjectRequest
            {
                SourceBucket = srcBucket,
                SourceKey = "missing.txt",
                DestinationBucket = destBucket,
                DestinationKey = "dest.txt"
            });
        }
        catch (AmazonS3Exception e) { ex = e; }

        await Assert.That(ex).IsNotNull();
        await Assert.That(ex!.ErrorCode).IsEqualTo("NoSuchKey");
    }

    [Test]
    public async Task DeleteObjects_ExistingKeys_Returns200AndRemovesObjects()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        await fixture.S3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = "a.txt", ContentBody = "a" });
        await fixture.S3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = "b.txt", ContentBody = "b" });
        await fixture.S3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = "c.txt", ContentBody = "c" });

        var deleteResponse = await fixture.S3.DeleteObjectsAsync(new DeleteObjectsRequest
        {
            BucketName = bucket,
            Objects = [new KeyVersion { Key = "a.txt" }, new KeyVersion { Key = "b.txt" }]
        });
        var deletedKeys = deleteResponse.DeletedObjects.Select(o => o.Key).ToList();

        await Assert.That(deleteResponse.HttpStatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(deletedKeys.Contains("a.txt")).IsTrue();
        await Assert.That(deletedKeys.Contains("b.txt")).IsTrue();

        var listResponse = await fixture.S3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket });
        var remainingKeys = (listResponse.S3Objects ?? []).Select(o => o.Key).ToList();

        await Assert.That(remainingKeys.Contains("c.txt")).IsTrue();
        await Assert.That(remainingKeys.Contains("a.txt")).IsFalse();
    }

    [Test]
    public async Task DeleteObjects_NonExistentKeys_Returns200Idempotent()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });

        var response = await fixture.S3.DeleteObjectsAsync(new DeleteObjectsRequest
        {
            BucketName = bucket,
            Objects = [new KeyVersion { Key = "missing.txt" }]
        });
        var deletedKeys = response.DeletedObjects.Select(o => o.Key).ToList();

        await Assert.That(response.HttpStatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(deletedKeys.Contains("missing.txt")).IsTrue();
    }

    [Test]
    public async Task ListObjectsV2_EmptyBucket_ReturnsZeroKeyCount()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });

        var response = await fixture.S3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket });

        await Assert.That(response.HttpStatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.KeyCount).IsEqualTo(0);
        await Assert.That(response.S3Objects ?? []).IsEmpty();
    }

    [Test]
    public async Task ListObjectsV2_WithObjects_ReturnsAllKeys()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        await fixture.S3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = "a.txt", ContentBody = "a" });
        await fixture.S3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = "b.txt", ContentBody = "b" });

        var response = await fixture.S3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket });
        var keys = (response.S3Objects ?? []).Select(o => o.Key).ToList();

        await Assert.That(keys.Contains("a.txt")).IsTrue();
        await Assert.That(keys.Contains("b.txt")).IsTrue();
    }

    [Test]
    public async Task ListObjectsV2_WithPrefix_FiltersKeys()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        await fixture.S3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = "logs/a.log", ContentBody = "a" });
        await fixture.S3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = "logs/b.log", ContentBody = "b" });
        await fixture.S3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = "docs/readme.txt", ContentBody = "c" });

        var response = await fixture.S3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket, Prefix = "logs/" });
        var keys = (response.S3Objects ?? []).Select(o => o.Key).ToList();

        await Assert.That(keys.Contains("logs/a.log")).IsTrue();
        await Assert.That(keys.Contains("logs/b.log")).IsTrue();
        await Assert.That(keys.Contains("docs/readme.txt")).IsFalse();
    }

    [Test]
    public async Task ListObjectsV2_WithDelimiter_GroupsCommonPrefixes()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        await fixture.S3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = "logs/2024/01.log", ContentBody = "a" });
        await fixture.S3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = "logs/2024/02.log", ContentBody = "b" });

        var response = await fixture.S3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket, Delimiter = "/" });

        await Assert.That(response.CommonPrefixes.Contains("logs/")).IsTrue();
        await Assert.That(response.S3Objects ?? []).IsEmpty();
    }

    [Test]
    public async Task ListObjectsV2_MaxKeys_PaginatesResults()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        for (var i = 0; i < 5; i++)
            await fixture.S3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = $"file{i:D2}.txt", ContentBody = "x" });

        var firstPage = await fixture.S3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket, MaxKeys = 2 });

        await Assert.That(firstPage.IsTruncated).IsTrue();
        await Assert.That((firstPage.S3Objects ?? []).Count).IsEqualTo(2);
        await Assert.That(firstPage.NextContinuationToken).IsNotNull();
    }

    [Test]
    public async Task ListObjectsV2_ContinuationToken_ReturnsNextPage()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        for (var i = 0; i < 5; i++)
            await fixture.S3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = $"file{i:D2}.txt", ContentBody = "x" });

        var firstPage = await fixture.S3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket, MaxKeys = 2 });
        var secondPage = await fixture.S3.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = bucket,
            MaxKeys = 2,
            ContinuationToken = firstPage.NextContinuationToken
        });

        var firstKeys = (firstPage.S3Objects ?? []).Select(o => o.Key).ToHashSet();
        var secondKeys = (secondPage.S3Objects ?? []).Select(o => o.Key).ToList();

        await Assert.That(secondKeys.Any(k => firstKeys.Contains(k))).IsFalse();
    }

    [Test]
    public async Task ListObjectsV1_WithMarker_SkipsToAfterMarker()
    {
        var bucket = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        await fixture.S3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = "a.txt", ContentBody = "a" });
        await fixture.S3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = "b.txt", ContentBody = "b" });
        await fixture.S3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = "c.txt", ContentBody = "c" });

        var response = await fixture.S3.ListObjectsAsync(new ListObjectsRequest { BucketName = bucket, Marker = "a.txt" });
        var keys = response.S3Objects.Select(o => o.Key).ToList();

        await Assert.That(keys.Contains("a.txt")).IsFalse();
        await Assert.That(keys.Contains("b.txt")).IsTrue();
        await Assert.That(keys.Contains("c.txt")).IsTrue();
    }

    [Test]
    public async Task ListObjectsV2_NonExistentBucket_ThrowsNoSuchBucket()
    {
        AmazonS3Exception? ex = null;
        try { await fixture.S3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = UniqueName() }); }
        catch (AmazonS3Exception e) { ex = e; }

        await Assert.That(ex).IsNotNull();
        await Assert.That(ex!.ErrorCode).IsEqualTo("NoSuchBucket");
    }
}
