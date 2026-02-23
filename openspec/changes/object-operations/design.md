# Object Operations — Design

## aws-chunked Decoding

PutObject must decode `aws-chunked` framing when `x-amz-content-sha256` starts with `STREAMING-` or `x-amz-decoded-content-length` is present. Decoding reads hex-size/data chunk pairs until a zero-length terminator, then replaces the request body with decoded bytes.

## CopyObject Routing

`HandlePutOrCopyObject` checks for the `x-amz-copy-source` header to decide between PutObject and CopyObject paths. The copy source value is in the format `/sourceBucket/sourceKey` or `sourceBucket/sourceKey`.

## Metadata

User metadata is read from request headers prefixed `x-amz-meta-*` and stored on the `S3Object`.

## Response Formats

- PutObject: 200 OK with `ETag` header
- GetObject: 200 OK with `Content-Type`, `ETag`, `Last-Modified`, `Content-Length` headers + body
- HeadObject: 200 OK with same headers, no body
- DeleteObject: 204 No Content
- CopyObject: 200 OK with `CopyObjectResult` XML
- DeleteObjects: 200 OK with `DeleteResult` XML
