# AWS SDK v4 Compatibility Specification

## Purpose

Document the compatibility shims required to make the AWS SDK v4 for .NET work correctly with an in-process test server (WebApplicationFactory / TestServer).

These shims SHALL exist only in the test fixture layer. The production server code SHALL NOT contain any SDK-specific workarounds.

## Requirements

### Requirement: URI Normalization Handler

The test fixture SHALL include a `DelegatingHandler` named `UriNormalizingHandler` that re-creates request URIs from their `OriginalString` before forwarding.

**Background:** AWS SDK v4 creates `Uri` objects using `UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = true }`. The ASP.NET Core `TestServer.ClientHandler` calls `uri.GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped)` internally, which throws a `UriFormatException` on such URIs.

**Fix:** Before forwarding the request, reconstruct the URI using:

```csharp
request.RequestUri = new Uri(request.RequestUri!.OriginalString);
```

#### Scenario: SDK sends a request with a non-canonical URI

- GIVEN the AWS SDK creates a `Uri` with disabled path canonicalization
- WHEN `PutObject` or any SDK operation is called via the in-process test client
- THEN the `UriNormalizingHandler` re-creates the URI from `OriginalString`
- THEN the request reaches the ASP.NET Core pipeline without throwing

### Requirement: aws-chunked Body Decoding

The `UriNormalizingHandler` (or a companion handler) SHALL detect and decode `aws-chunked` encoded request bodies before they reach the server.

**Background:** AWS SDK v4 uses `STREAMING-AWS4-HMAC-SHA256-PAYLOAD-TRAILER` transfer encoding by default for PutObject. The in-process `TestServer` bypasses HTTP framing, so the raw chunk-signed bytes reach the server instead of the decoded content.

**Detection:** A request body is aws-chunked if either:

- The `x-amz-content-sha256` header value starts with `STREAMING-`
- OR the `x-amz-decoded-content-length` request header is present

**Decoding:** Each chunk has the format:

```
<hex-size>[;chunk-signature=...]\r\n
<chunk-data>\r\n
```

The handler SHALL read and decode all chunks until a zero-length chunk is reached, then replace the request body with the decoded bytes.

#### Scenario: SDK sends aws-chunked PutObject

- GIVEN AWS SDK v4 sends a PutObject request with `x-amz-content-sha256: STREAMING-AWS4-HMAC-SHA256-PAYLOAD-TRAILER`
- WHEN the handler processes the request
- THEN the raw chunked body is decoded to the original content bytes
- THEN the server receives and stores the correct content

### Requirement: Handler Chain

The `UriNormalizingHandler` SHALL be inserted into the `HttpClient` handler chain between the `AmazonS3Client` and the `TestServer.ClientHandler`:

```
AmazonS3Client → UriNormalizingHandler → TestServer.ClientHandler
```

### Requirement: Scope

These handlers apply only to `MockS3.Tests.Spec`. The `MockS3.Tests.Rclone` project does not require them (rclone connects via a real TCP socket, so normal HTTP framing applies).
