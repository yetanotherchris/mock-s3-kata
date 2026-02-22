# Object Listing Specification

## Purpose

Define the behavior of S3 object listing operations, covering both the V1 and V2 APIs with prefix filtering, delimiter grouping, and pagination.

## Requirements

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
