# Buckets Specification

## Purpose

Define the behavior of bucket-level S3 API operations: CreateBucket, DeleteBucket, HeadBucket, and ListBuckets.

## Requirements

### Requirement: CreateBucket

`PUT /{bucket}` SHALL create a bucket if it does not exist, or succeed idempotently if it does.

#### Scenario: Create new bucket

- GIVEN no bucket with the given name exists
- WHEN `PUT /my-bucket` is received
- THEN the bucket is created in storage
- THEN the server responds with `200 OK` and a `Location: /my-bucket` header

#### Scenario: Create bucket that already exists

- GIVEN a bucket named `my-bucket` already exists
- WHEN `PUT /my-bucket` is received again
- THEN the server responds with `200 OK` (idempotent, no error)

### Requirement: DeleteBucket

`DELETE /{bucket}` SHALL delete a bucket.

#### Scenario: Delete existing empty bucket

- GIVEN a bucket exists and contains no objects
- WHEN `DELETE /my-bucket` is received
- THEN the bucket is removed from storage
- THEN the server responds with `204 No Content`

#### Scenario: Delete non-existent bucket

- GIVEN no bucket with the given name exists
- WHEN `DELETE /my-bucket` is received
- THEN the server responds with `404 Not Found` and an AWS XML error body with `<Code>NoSuchBucket</Code>`

#### Scenario: Delete non-empty bucket

- GIVEN a bucket exists and contains at least one object
- WHEN `DELETE /my-bucket` is received
- THEN the server responds with `409 Conflict` and an AWS XML error body with `<Code>BucketNotEmpty</Code>`

### Requirement: HeadBucket

`HEAD /{bucket}` SHALL check bucket existence without returning a body.

#### Scenario: Head existing bucket

- GIVEN a bucket exists
- WHEN `HEAD /my-bucket` is received
- THEN the server responds with `200 OK` and no body

#### Scenario: Head non-existent bucket

- GIVEN no bucket with the given name exists
- WHEN `HEAD /my-bucket` is received
- THEN the server responds with `404 Not Found` and no body

### Requirement: ListBuckets

`GET /` SHALL return a list of all buckets.

#### Scenario: List all buckets

- GIVEN buckets `alpha` and `beta` exist
- WHEN `GET /` is received
- THEN the server responds with `200 OK` and an XML body containing both bucket names and their creation dates

#### Scenario: List when no buckets exist

- GIVEN no buckets have been created
- WHEN `GET /` is received
- THEN the server responds with `200 OK` and an XML body with an empty bucket list

The XML response SHALL follow the AWS ListBuckets response format:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<ListAllMyBucketsResult>
  <Buckets>
    <Bucket>
      <Name>alpha</Name>
      <CreationDate>2024-01-01T00:00:00.000Z</CreationDate>
    </Bucket>
  </Buckets>
</ListAllMyBucketsResult>
```
