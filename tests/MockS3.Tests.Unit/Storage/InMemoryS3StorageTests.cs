using MockS3.Storage;

namespace MockS3.Tests.Unit.Storage;

public class InMemoryS3StorageTests
{
    [Test]
    public async Task GetOrCreateBucket_WhenBucketDoesNotExist_RegistersBucket()
    {
        var storage = new InMemoryS3Storage();

        var bucket = storage.GetOrCreateBucket("my-bucket");

        await Assert.That(bucket.Name).IsEqualTo("my-bucket");
    }

    [Test]
    public async Task GetOrCreateBucket_WhenBucketAlreadyExists_ReturnsSameInstance()
    {
        var storage = new InMemoryS3Storage();
        var first = storage.GetOrCreateBucket("my-bucket");

        var second = storage.GetOrCreateBucket("my-bucket");

        await Assert.That(ReferenceEquals(first, second)).IsTrue();
    }

    [Test]
    public async Task GetOrCreateBucket_SetsCreatedAt()
    {
        var before = DateTimeOffset.UtcNow;
        var storage = new InMemoryS3Storage();

        var bucket = storage.GetOrCreateBucket("my-bucket");

        await Assert.That(bucket.CreatedAt).IsGreaterThanOrEqualTo(before);
    }

    [Test]
    public async Task PutObject_ObjectsAreIsolatedBetweenBuckets()
    {
        var storage = new InMemoryS3Storage();
        storage.GetOrCreateBucket("a");
        storage.GetOrCreateBucket("b");
        storage.PutObject("a", "file.txt", [1, 2, 3], "application/octet-stream", new Dictionary<string, string>());

        storage.TryGetBucket("b", out var bucketB);

        await Assert.That(bucketB!.Objects.ContainsKey("file.txt")).IsFalse();
    }

    [Test]
    public async Task PutObject_ComputesCorrectETag()
    {
        var storage = new InMemoryS3Storage();
        storage.GetOrCreateBucket("my-bucket");

        var obj = storage.PutObject("my-bucket", "key", [], "application/octet-stream", new Dictionary<string, string>());

        await Assert.That(obj.ETag).IsEqualTo("\"d41d8cd98f00b204e9800998ecf8427e\"");
    }

    [Test]
    public async Task PutObject_SetsContentLength()
    {
        var storage = new InMemoryS3Storage();
        storage.GetOrCreateBucket("my-bucket");

        var obj = storage.PutObject("my-bucket", "key", [1, 2, 3, 4, 5], "application/octet-stream", new Dictionary<string, string>());

        await Assert.That(obj.ContentLength).IsEqualTo(5L);
    }

    [Test]
    public async Task PutObject_StoresUserMetadata()
    {
        var storage = new InMemoryS3Storage();
        storage.GetOrCreateBucket("my-bucket");
        var metadata = new Dictionary<string, string> { ["x-amz-meta-author"] = "test" };

        var obj = storage.PutObject("my-bucket", "key", [], "application/octet-stream", metadata);

        await Assert.That(obj.UserMetadata["x-amz-meta-author"]).IsEqualTo("test");
    }

    [Test]
    public async Task DeleteBucket_RemovesBucketFromStorage()
    {
        var storage = new InMemoryS3Storage();
        storage.GetOrCreateBucket("my-bucket");

        storage.DeleteBucket("my-bucket");

        await Assert.That(storage.TryGetBucket("my-bucket", out _)).IsFalse();
    }

    [Test]
    public async Task DeleteBucket_RemovesObjectsWithBucket()
    {
        var storage = new InMemoryS3Storage();
        storage.GetOrCreateBucket("my-bucket");
        storage.PutObject("my-bucket", "key", [1], "application/octet-stream", new Dictionary<string, string>());

        storage.DeleteBucket("my-bucket");

        await Assert.That(storage.TryGetBucket("my-bucket", out _)).IsFalse();
    }
}
