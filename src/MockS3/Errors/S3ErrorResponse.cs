using System.Text;
using System.Xml;

namespace MockS3.Errors;

public sealed record S3ErrorResponse(
    int StatusCode,
    string Code,
    string Message,
    string Resource = "",
    string RequestId = "mock-request-id")
{
    public static S3ErrorResponse NoSuchBucket(string resource) =>
        new(404, "NoSuchBucket", "The specified bucket does not exist", resource);

    public static S3ErrorResponse NoSuchKey(string resource) =>
        new(404, "NoSuchKey", "The specified key does not exist.", resource);

    public static S3ErrorResponse BucketNotEmpty(string resource) =>
        new(409, "BucketNotEmpty", "The bucket you tried to delete is not empty.", resource);

    public string ToXml()
    {
        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        }))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("Error");
            writer.WriteElementString("Code", Code);
            writer.WriteElementString("Message", Message);
            writer.WriteElementString("Resource", Resource);
            writer.WriteElementString("RequestId", RequestId);
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public async Task WriteAsync(HttpResponse response, bool writeBody = true)
    {
        response.StatusCode = StatusCode;
        response.ContentType = "application/xml";
        if (writeBody)
            await response.WriteAsync(ToXml());
    }
}
