# Error Responses Specification

## Purpose

Define the format of error responses returned by the mock S3 server, following the AWS S3 XML error format.

## Requirements

### Requirement: AWS XML Error Format

All error responses (except HEAD requests) SHALL include an XML body in the following format:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Error>
  <Code>ErrorCode</Code>
  <Message>Human-readable description</Message>
  <Resource>/bucket/key</Resource>
  <RequestId>mock-request-id</RequestId>
</Error>
```

- `<Code>` — the AWS error code string (see table below)
- `<Message>` — a short human-readable description
- `<Resource>` — the request path (bucket and/or key)
- `<RequestId>` — a fixed or generated identifier; `mock-request-id` is acceptable

### Requirement: Content-Type Header

Error responses SHALL include the `Content-Type: application/xml` response header.

### Requirement: Standard Error Codes

The server SHALL use the following error codes:

| HTTP Status | Error Code | Trigger |
|-------------|------------|---------|
| 404 | `NoSuchBucket` | Requested bucket does not exist |
| 404 | `NoSuchKey` | Requested object key does not exist |
| 409 | `BucketNotEmpty` | Attempt to delete a non-empty bucket |

### Requirement: HEAD Requests Return No Body

`HEAD` request errors SHALL NOT include an XML body, even when the equivalent `GET` would.

#### Scenario: HeadBucket for non-existent bucket

- GIVEN no bucket with the given name exists
- WHEN `HEAD /missing-bucket` is received
- THEN the server responds with `404 Not Found`
- THEN the response body is empty

#### Scenario: HeadObject for non-existent key

- GIVEN an object does not exist
- WHEN `HEAD /my-bucket/missing` is received
- THEN the server responds with `404 Not Found`
- THEN the response body is empty

### Requirement: Successful Responses Are Not XML

Success responses (2xx) SHALL use plain XML only where the AWS spec requires it (e.g. ListBuckets, ListObjects, CopyObject, DeleteObjects). They SHALL NOT wrap success in an `<Error>` element.
