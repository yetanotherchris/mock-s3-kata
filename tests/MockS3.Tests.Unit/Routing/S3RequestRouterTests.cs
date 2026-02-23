using Microsoft.AspNetCore.Http;
using MockS3.Routing;

namespace MockS3.Tests.Unit.Routing;

public class S3RequestRouterTests
{
    [Test]
    public async Task GetNonEmptyQueryParam_WhenValueIsPresent_ReturnsValue()
    {
        var query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["delimiter"] = "/"
        });

        var result = S3RequestRouter.GetNonEmptyQueryParam(query, "delimiter");

        await Assert.That(result).IsEqualTo("/");
    }

    [Test]
    public async Task GetNonEmptyQueryParam_WhenValueIsEmptyString_ReturnsNull()
    {
        var query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["delimiter"] = ""
        });

        var result = S3RequestRouter.GetNonEmptyQueryParam(query, "delimiter");

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetNonEmptyQueryParam_WhenKeyIsAbsent_ReturnsNull()
    {
        var query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

        var result = S3RequestRouter.GetNonEmptyQueryParam(query, "delimiter");

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetNonEmptyQueryParam_WhenPrefixIsEmptyString_ReturnsNull()
    {
        var query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["prefix"] = ""
        });

        var result = S3RequestRouter.GetNonEmptyQueryParam(query, "prefix");

        await Assert.That(result).IsNull();
    }
}
