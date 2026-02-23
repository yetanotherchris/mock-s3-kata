# Objects Specification

## Purpose

Define the behavior of object-level S3 API operations: PutObject, GetObject, HeadObject, DeleteObject, CopyObject, DeleteObjects (batch delete), and object listing (ListObjectsV1 and ListObjectsV2).

---

## Object Operations

### Requirement: PutObject

`PUT /{bucket}/{key+}` SHALL store an object in the specified bucket.

#### Scenario: Store object in existing bucket

- GIVEN a bucket exists
- WHEN `PUT /my-bucket/path/to/object` is received with a body
- THEN the object is stored with the request body as content
- THEN the server responds with `200 OK` and an `ETag` response header (quoted MD5 of the stored content)

#### Scenario: Store object with Content-Type header

- GIVEN a bucket exists
- WHEN `PUT /my-bucket/key` is received with `Content-Type: text/plain`
- THEN the stored object has `ContentType` set to `text/plain`

#### Scenario: Store object with user metadata

- GIVEN a bucket exists
- WHEN `PUT /my-bucket/key` is received with `x-amz-meta-author: alice`
- THEN the stored object records `author: alice` in its user metadata

#### Scenario: Store object in non-existent bucket

- GIVEN no bucket with the given name exists
- WHEN `PUT /my-bucket/key` is received
- THEN the server responds with `404 Not Found` and an AWS XML error with `<Code>NoSuchBucket</Code>`

#### Scenario: aws-chunked encoded body

- GIVEN the request includes an `x-amz-content-sha256` header starting with `STREAMING-` or an `x-amz-decoded-content-length` header
- WHEN `PUT /my-bucket/key` is received
- THEN the handler decodes the aws-chunked framing and stores only the decoded content bytes

### Requirement: GetObject

`GET /{bucket}/{key+}` SHALL retrieve the stored object and its metadata.

#### Scenario: Get existing object

- GIVEN an object with known content exists in a bucket
- WHEN `GET /my-bucket/key` is received
- THEN the server responds with `200 OK`
- THEN the response includes `Content-Type`, `ETag`, `Last-Modified`, and `Content-Length` headers
- THEN the response body equals the stored content bytes

#### Scenario: Get non-existent object

- GIVEN a bucket exists but the requested key does not
- WHEN `GET /my-bucket/missing` is received
- THEN the server responds with `404 Not Found` and an AWS XML error with `<Code>NoSuchKey</Code>`

#### Scenario: Get object from non-existent bucket

- GIVEN no bucket with the given name exists
- WHEN `GET /missing-bucket/key` is received
- THEN the server responds with `404 Not Found` and an AWS XML error with `<Code>NoSuchBucket</Code>`

### Requirement: HeadObject

`HEAD /{bucket}/{key+}` SHALL return object metadata headers without a body.

#### Scenario: Head existing object

- GIVEN a bucket and object exist
- WHEN `HEAD /my-bucket/key` is received
- THEN the server responds with `200 OK`
- THEN the response includes `Content-Length`, `Content-Type`, `ETag`, and `Last-Modified` headers
- THEN the response body is empty

#### Scenario: Head non-existent object

- GIVEN the object does not exist
- WHEN `HEAD /my-bucket/missing` is received
- THEN the server responds with `404 Not Found` and no body

### Requirement: DeleteObject

`DELETE /{bucket}/{key+}` SHALL remove an object from a bucket.

#### Scenario: Delete existing object

- GIVEN a bucket and object exist
- WHEN `DELETE /my-bucket/key` is received
- THEN the object is removed from storage
- THEN the server responds with `204 No Content`

#### Scenario: Delete non-existent object (idempotent)

- GIVEN a bucket exists but the key does not
- WHEN `DELETE /my-bucket/missing` is received
- THEN the server responds with `204 No Content` (S3 deletes are idempotent)

### Requirement: CopyObject

`PUT /{bucket}/{destKey+}` with an `x-amz-copy-source` header SHALL copy an object from the source to the destination.

The `x-amz-copy-source` value SHALL be in the format `/sourceBucket/sourceKey` or `sourceBucket/sourceKey`.

#### Scenario: Copy existing object

- GIVEN both source and destination buckets exist and the source object exists
- WHEN `PUT /dest-bucket/dest-key` is received with `x-amz-copy-source: /src-bucket/src-key`
- THEN the source object's content and metadata are copied to the destination key
- THEN the server responds with `200 OK` and an XML body containing the new `ETag` and `LastModified`

#### Scenario: Copy non-existent source object

- GIVEN the source key does not exist
- WHEN the CopyObject request is received
- THEN the server responds with `404 Not Found` and an AWS XML error with `<Code>NoSuchKey</Code>`

### Requirement: DeleteObjects (Batch)

`POST /{bucket}?delete` SHALL delete multiple objects specified in an XML request body.

The request body format:

```xml
<Delete>
  <Object><Key>key1</Key></Object>
  <Object><Key>key2</Key></Object>
</Delete>
```

#### Scenario: Delete multiple existing objects

- GIVEN a bucket contains `key1`, `key2`, and `key3`
- WHEN `POST /my-bucket?delete` is received with a body specifying `key1` and `key2`
- THEN `key1` and `key2` are removed from storage
- THEN `key3` remains
- THEN the server responds with `200 OK` and an XML body listing the deleted keys

#### Scenario: Delete includes non-existent keys

- GIVEN a bucket exists and `key1` does not exist
- WHEN `POST /my-bucket?delete` specifies `key1`
- THEN the server responds with `200 OK` and includes `key1` in the deleted list (idempotent)

The response XML format:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<DeleteResult>
  <Deleted>
    <Key>key1</Key>
  </Deleted>
</DeleteResult>
```

---

## Object Listing

### Requirement: ListObjectsV1

`GET /{bucket}` without `list-type=2` SHALL return an object listing in the AWS ListObjects V1 XML format.

#### Scenario: List all objects in a bucket

- GIVEN a bucket contains `a.txt`, `b.txt`, and `c.txt`
- WHEN `GET /my-bucket` is received
- THEN the server responds with `200 OK`
- THEN the XML body contains all three keys with their sizes, ETags, and last-modified dates

#### Scenario: List empty bucket

- GIVEN a bucket exists but contains no objects
- WHEN `GET /my-bucket` is received
- THEN the server responds with `200 OK`
- THEN the XML body contains an empty object list and `<KeyCount>0</KeyCount>`

#### Scenario: List non-existent bucket

- GIVEN no bucket with the given name exists
- WHEN `GET /my-bucket` is received
- THEN the server responds with `404 Not Found` and an AWS XML error with `<Code>NoSuchBucket</Code>`

The V1 XML response format:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<ListBucketResult>
  <Name>my-bucket</Name>
  <Prefix></Prefix>
  <MaxKeys>1000</MaxKeys>
  <IsTruncated>false</IsTruncated>
  <Contents>
    <Key>a.txt</Key>
    <LastModified>2024-01-01T00:00:00.000Z</LastModified>
    <ETag>"abc123"</ETag>
    <Size>42</Size>
  </Contents>
</ListBucketResult>
```

### Requirement: ListObjectsV2

`GET /{bucket}?list-type=2` SHALL return an object listing in the AWS ListObjects V2 XML format.

The V2 format differs from V1 in:

- Uses `<KeyCount>` instead of counting `<Contents>` elements
- Supports `continuation-token` instead of `marker` for pagination
- Returns `<NextContinuationToken>` when truncated

#### Scenario: List with continuation token

- GIVEN a bucket contains 5 objects and `max-keys=2`
- WHEN `GET /my-bucket?list-type=2&max-keys=2` is received
- THEN the response contains 2 objects and `<IsTruncated>true</IsTruncated>` and `<NextContinuationToken>`

#### Scenario: Continue listing

- GIVEN a previous response included `<NextContinuationToken>TOKEN</NextContinuationToken>`
- WHEN `GET /my-bucket?list-type=2&continuation-token=TOKEN` is received
- THEN the server returns the next page of results

### Requirement: Prefix Filtering

Both V1 and V2 SHALL support a `prefix` query parameter to filter keys to those starting with the given string.

#### Scenario: List with prefix

- GIVEN a bucket contains `logs/2024/01.log`, `logs/2024/02.log`, and `docs/readme.txt`
- WHEN `GET /my-bucket?prefix=logs/` is received
- THEN only the two `logs/` objects are included in the response

#### Scenario: Empty prefix parameter

- GIVEN the request URL contains `prefix=` (empty string value)
- WHEN the listing is processed
- THEN `prefix` is treated as absent and all objects are returned

### Requirement: Delimiter Grouping

Both V1 and V2 SHALL support a `delimiter` query parameter. Keys that contain the delimiter after the prefix are grouped into `<CommonPrefixes>` rather than listed individually.

#### Scenario: List with slash delimiter

- GIVEN a bucket contains `logs/2024/01.log` and `logs/2024/02.log`
- WHEN `GET /my-bucket?delimiter=/` is received
- THEN the response contains `<CommonPrefixes><Prefix>logs/</Prefix></CommonPrefixes>`
- THEN no individual object entries appear for those keys

#### Scenario: Empty delimiter parameter

- GIVEN the request URL contains `delimiter=` (empty string value)
- WHEN the listing is processed
- THEN delimiter is treated as absent and no grouping occurs
- THEN all objects are listed individually

### Requirement: MaxKeys Pagination

Both V1 and V2 SHALL support a `max-keys` query parameter. The default is `1000`. Values above `1000` SHALL be clamped to `1000`.

#### Scenario: MaxKeys limits results

- GIVEN a bucket contains 10 objects
- WHEN `GET /my-bucket?max-keys=3` is received
- THEN the response contains exactly 3 objects
- THEN `<IsTruncated>true</IsTruncated>` is present

#### Scenario: Default MaxKeys

- GIVEN a bucket contains 500 objects
- WHEN `GET /my-bucket` is received without `max-keys`
- THEN all 500 objects are returned in a single non-truncated response

### Requirement: Marker (V1 Pagination)

ListObjectsV1 SHALL support a `marker` query parameter to resume listing after a given key.

#### Scenario: V1 listing with marker

- GIVEN a bucket contains `a`, `b`, `c`
- WHEN `GET /my-bucket?marker=a` is received
- THEN the response contains `b` and `c` only
