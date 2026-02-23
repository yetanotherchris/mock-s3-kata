# Server Specification

## Purpose

Define how the server formats error responses and dispatches incoming HTTP requests to S3 operation handlers.

---

## Error Responses

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

---

## HTTP Routing

### Requirement: Route Table

The server SHALL dispatch requests according to the following table:

| Method | Path | Query | Operation |
|--------|------|-------|-----------|
| GET | `/` | — | ListBuckets |
| PUT | `/{bucket}` | — | CreateBucket |
| DELETE | `/{bucket}` | — | DeleteBucket |
| HEAD | `/{bucket}` | — | HeadBucket |
| GET | `/{bucket}` | `list-type=2` | ListObjectsV2 |
| GET | `/{bucket}` | (no `list-type`) | ListObjectsV1 |
| POST | `/{bucket}` | `delete` | DeleteObjects |
| PUT | `/{bucket}/{key+}` | — | PutObject |
| PUT | `/{bucket}/{key+}` | — | CopyObject (when `x-amz-copy-source` header present) |
| GET | `/{bucket}/{key+}` | — | GetObject |
| DELETE | `/{bucket}/{key+}` | — | DeleteObject |
| HEAD | `/{bucket}/{key+}` | — | HeadObject |

### Requirement: CopyObject Detection

`PUT /{bucket}/{key+}` SHALL be routed to CopyObject when the `x-amz-copy-source` request header is present, and to PutObject otherwise.

### Requirement: DeleteObjects Detection

`POST /{bucket}` SHALL be routed to DeleteObjects when the `delete` query parameter key is present (regardless of its value).

### Requirement: Empty Query Parameters Treated as Absent

Query parameters present with an empty string value (e.g. `delimiter=` or `prefix=`) SHALL be treated as not provided.

They SHALL NOT be used as filter values.

#### Scenario: Rclone sends empty delimiter

- GIVEN rclone sends `GET /my-bucket?delimiter=&prefix=`
- WHEN the listing handler processes the request
- THEN both `delimiter` and `prefix` are treated as absent
- THEN all objects in the bucket are returned without grouping

### Requirement: Path-Style Addressing Only

The server SHALL support path-style S3 URLs (`/{bucket}/{key}`) only. Virtual-hosted-style URLs are not required.

### Requirement: Unmatched Routes

Requests that do not match any defined route SHALL receive a `405 Method Not Allowed` or `404 Not Found` response.

---

## Request Router Implementation

### Requirement: Constructor Injection

`S3RequestRouter` SHALL be a non-static class. `InMemoryS3Storage` SHALL be injected via the constructor and stored as a private field. It SHALL be registered in the DI container and resolved at startup, not per-request.

### Requirement: Handler Signatures

Each route handler SHALL accept:

- `HttpContext` for all HTTP concerns (request headers, query string, response headers, response status)
- Route template tokens (e.g. `string bucket`, `string key`) as typed method parameters

Handlers SHALL NOT take DI services, `HttpRequest`, or `HttpResponse` as separate method parameters.

### Requirement: Register Method

`S3RequestRouter` SHALL expose an instance method `Register(WebApplication app)` that maps all routes. `Program.cs` SHALL resolve the router from the DI container and call `Register`.
