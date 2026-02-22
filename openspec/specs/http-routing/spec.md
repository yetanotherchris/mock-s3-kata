# HTTP Routing Specification

## Purpose

Define how incoming HTTP requests are parsed and dispatched to the correct S3 operation handler based on method, path, and query parameters.

## Requirements

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
