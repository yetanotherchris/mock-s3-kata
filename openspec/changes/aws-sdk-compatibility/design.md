# AWS SDK Compatibility — Design

## UriNormalizingHandler

A `DelegatingHandler` inserted between `AmazonS3Client` and `TestServer.ClientHandler`.

**URI fix:** Reconstructs the URI from `OriginalString` before forwarding:
```csharp
request.RequestUri = new Uri(request.RequestUri!.OriginalString);
```

**aws-chunked fix:** Detects chunked bodies when `x-amz-content-sha256` starts with `STREAMING-` or `x-amz-decoded-content-length` header is present. Reads hex-size/data chunk pairs until zero-length terminator, replaces request body with decoded bytes.

## Handler Chain

```
AmazonS3Client → UriNormalizingHandler → TestServer.ClientHandler
```

## Location

`tests/MockS3.Tests.Spec/Fixtures/UriNormalizingHandler.cs`
