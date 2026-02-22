# In-Memory Storage Specification

## Purpose

Define how S3 buckets and objects are stored in memory during server operation. All data is volatile and lost on server restart.

## Requirements

### Requirement: Thread-Safe Storage

The storage layer SHALL use thread-safe data structures to support concurrent requests without locking at the handler level. `ConcurrentDictionary` is the expected primitive.

### Requirement: Bucket Registry

The storage SHALL maintain a top-level registry of buckets keyed by bucket name.

#### Scenario: Bucket creation

- GIVEN no bucket with the given name exists
- WHEN CreateBucket is called with a valid bucket name
- THEN the bucket is registered in storage with a `CreatedAt` timestamp

#### Scenario: Duplicate bucket

- GIVEN a bucket named `my-bucket` already exists
- WHEN CreateBucket is called again with the same name
- THEN the existing bucket is returned unchanged (idempotent)

### Requirement: Per-Bucket Object Store

Each bucket SHALL maintain its own collection of objects keyed by object key (string).

#### Scenario: Object isolation between buckets

- GIVEN two buckets `a` and `b` both contain a key `file.txt`
- WHEN `GetObject` is called on bucket `a`
- THEN only the object from bucket `a` is returned

### Requirement: Stored Object Model

Each stored object SHALL include:

- `Key` — string, the object key
- `Content` — `byte[]`, the raw object bytes
- `ContentType` — string (default: `application/octet-stream`)
- `ETag` — MD5 hex digest of `Content`, formatted as a quoted string (e.g. `"d41d8cd98f00b204e9800998ecf8427e"`)
- `LastModified` — `DateTimeOffset` set at the time of storage
- `ContentLength` — `long`, length of `Content`
- `UserMetadata` — `IReadOnlyDictionary<string, string>` of `x-amz-meta-*` header values

#### Scenario: PutObject stores correct ETag

- GIVEN an object is stored with known content
- WHEN the ETag is computed
- THEN it equals the MD5 hex digest of the content, wrapped in double quotes

### Requirement: Bucket Deletion Removes Objects

Deleting a bucket SHALL remove the bucket and all objects it contains from storage.

### Requirement: No Persistence

The storage layer SHALL NOT write to disk. There is no restart recovery.
