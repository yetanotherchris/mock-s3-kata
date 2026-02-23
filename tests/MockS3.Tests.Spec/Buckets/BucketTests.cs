using Amazon.S3;
using Amazon.S3.Model;
using MockS3.Tests.Spec.Fixtures;

namespace MockS3.Tests.Spec.Buckets;

[ClassDataSource<MockS3Fixture>(Shared = SharedType.PerClass)]
public class BucketTests(MockS3Fixture fixture)
{
    [Test]
    public async Task CreateBucket_NewBucket_Returns200()
    {
        var bucketName = UniqueName();

        var response = await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

        await Assert.That(response.HttpStatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
    }

    [Test]
    public async Task CreateBucket_NewBucket_SetsLocationHeader()
    {
        var bucketName = UniqueName();

        var response = await fixture.Http.PutAsync($"/{bucketName}", null);

        await Assert.That(response.Headers.Location?.ToString()).IsEqualTo($"/{bucketName}");
    }

    [Test]
    public async Task CreateBucket_ExistingBucket_ReturnsOk()
    {
        var bucketName = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

        var response = await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

        await Assert.That(response.HttpStatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
    }

    [Test]
    public async Task DeleteBucket_ExistingEmptyBucket_Returns204()
    {
        var bucketName = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

        var response = await fixture.Http.DeleteAsync($"/{bucketName}");

        await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeleteBucket_NonExistentBucket_Returns404WithNoSuchBucket()
    {
        var bucketName = UniqueName();

        AmazonS3Exception? ex = null;
        try { await fixture.S3.DeleteBucketAsync(new DeleteBucketRequest { BucketName = bucketName }); }
        catch (AmazonS3Exception e) { ex = e; }

        await Assert.That(ex).IsNotNull();
        await Assert.That(ex!.ErrorCode).IsEqualTo("NoSuchBucket");
    }

    [Test]
    public async Task DeleteBucket_NonEmptyBucket_Returns409WithBucketNotEmpty()
    {
        var bucketName = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });
        // PutObject not yet implemented; pre-populate via storage directly
        fixture.Storage.PutObject(bucketName, "test.txt", [1, 2, 3], "text/plain", new Dictionary<string, string>());

        AmazonS3Exception? ex = null;
        try { await fixture.S3.DeleteBucketAsync(new DeleteBucketRequest { BucketName = bucketName }); }
        catch (AmazonS3Exception e) { ex = e; }

        await Assert.That(ex).IsNotNull();
        await Assert.That(ex!.ErrorCode).IsEqualTo("BucketNotEmpty");
    }

    [Test]
    public async Task HeadBucket_ExistingBucket_Returns200()
    {
        var bucketName = UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

        using var request = new HttpRequestMessage(HttpMethod.Head, $"/{bucketName}");
        var response = await fixture.Http.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
    }

    [Test]
    public async Task HeadBucket_NonExistentBucket_Returns404()
    {
        var bucketName = UniqueName();

        using var request = new HttpRequestMessage(HttpMethod.Head, $"/{bucketName}");
        var response = await fixture.Http.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ListBuckets_MultipleBuckets_ReturnsBothNames()
    {
        var alpha = "alpha-" + UniqueName();
        var beta = "beta-" + UniqueName();
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = alpha });
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = beta });

        var response = await fixture.S3.ListBucketsAsync();
        var names = (response.Buckets ?? []).Select(b => b.BucketName).ToList();

        await Assert.That(names.Contains(alpha)).IsTrue();
        await Assert.That(names.Contains(beta)).IsTrue();
    }

    [Test]
    public async Task ListBuckets_MultipleBuckets_IncludesCreationDate()
    {
        var bucketName = UniqueName();
        var before = DateTime.UtcNow.AddSeconds(-1);
        await fixture.S3.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

        var response = await fixture.S3.ListBucketsAsync();
        var bucket = response.Buckets.FirstOrDefault(b => b.BucketName == bucketName);

        await Assert.That(bucket).IsNotNull();
        await Assert.That(bucket!.CreationDate.GetValueOrDefault()).IsGreaterThanOrEqualTo(before);
    }

    [Test]
    public async Task ListBuckets_NoBuckets_ReturnsEmptyList()
    {
        // Use a fresh factory to get clean storage
        await using var fixture2 = new MockS3Fixture();

        var response = await fixture2.S3.ListBucketsAsync();

        await Assert.That(response.Buckets ?? []).IsEmpty();
    }

    private static string UniqueName() => Guid.NewGuid().ToString("N")[..16];
}
