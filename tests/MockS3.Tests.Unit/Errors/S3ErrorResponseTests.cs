using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using MockS3.Errors;

namespace MockS3.Tests.Unit.Errors;

public class S3ErrorResponseTests
{
    [Test]
    public async Task NoSuchBucket_Returns404WithCorrectCode()
    {
        var error = S3ErrorResponse.NoSuchBucket("/my-bucket");

        await Assert.That(error.StatusCode).IsEqualTo(404);
        await Assert.That(error.Code).IsEqualTo("NoSuchBucket");
    }

    [Test]
    public async Task NoSuchKey_Returns404WithCorrectCode()
    {
        var error = S3ErrorResponse.NoSuchKey("/my-bucket/my-key");

        await Assert.That(error.StatusCode).IsEqualTo(404);
        await Assert.That(error.Code).IsEqualTo("NoSuchKey");
    }

    [Test]
    public async Task BucketNotEmpty_Returns409WithCorrectCode()
    {
        var error = S3ErrorResponse.BucketNotEmpty("/my-bucket");

        await Assert.That(error.StatusCode).IsEqualTo(409);
        await Assert.That(error.Code).IsEqualTo("BucketNotEmpty");
    }

    [Test]
    public async Task ToXml_ContainsAllRequiredElements()
    {
        var error = S3ErrorResponse.NoSuchBucket("/my-bucket");

        var xml = error.ToXml();
        var doc = XDocument.Parse(xml);
        var root = doc.Root!;

        await Assert.That(root.Name.LocalName).IsEqualTo("Error");
        await Assert.That(root.Element("Code")!.Value).IsEqualTo("NoSuchBucket");
        await Assert.That(root.Element("Resource")!.Value).IsEqualTo("/my-bucket");
        await Assert.That(root.Element("RequestId")!.Value).IsEqualTo("mock-request-id");
        await Assert.That(root.Element("Message")).IsNotNull();
    }

    [Test]
    public async Task WriteAsync_SetsContentTypeToApplicationXml()
    {
        var error = S3ErrorResponse.NoSuchBucket("/my-bucket");
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await error.WriteAsync(context.Response);

        await Assert.That(context.Response.ContentType).IsEqualTo("application/xml");
    }

    [Test]
    public async Task WriteAsync_SetsCorrectStatusCode()
    {
        var error = S3ErrorResponse.NoSuchBucket("/my-bucket");
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await error.WriteAsync(context.Response);

        await Assert.That(context.Response.StatusCode).IsEqualTo(404);
    }

    [Test]
    public async Task WriteAsync_WithWriteBodyFalse_WritesNoBody()
    {
        var error = S3ErrorResponse.NoSuchBucket("/my-bucket");
        var context = new DefaultHttpContext();
        var body = new MemoryStream();
        context.Response.Body = body;

        await error.WriteAsync(context.Response, writeBody: false);

        await Assert.That(body.Length).IsEqualTo(0L);
    }

    [Test]
    public async Task WriteAsync_WithWriteBodyTrue_WritesXmlBody()
    {
        var error = S3ErrorResponse.NoSuchKey("/my-bucket/my-key");
        var context = new DefaultHttpContext();
        var body = new MemoryStream();
        context.Response.Body = body;

        await error.WriteAsync(context.Response, writeBody: true);

        await Assert.That(body.Length).IsGreaterThan(0L);
    }
}
