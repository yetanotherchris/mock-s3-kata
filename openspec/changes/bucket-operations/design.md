# Bucket Operations — Design

## Storage Integration

All handlers receive `InMemoryS3Storage` via minimal API parameter injection (DI).

## XML Serialisation

`ListBuckets` response uses `XmlWriter` consistent with `S3ErrorResponse`. Format follows the AWS `ListAllMyBucketsResult` envelope.

## Error Paths

- `DeleteBucket` on missing bucket → `S3ErrorResponse.NoSuchBucket`
- `DeleteBucket` on non-empty bucket → `S3ErrorResponse.BucketNotEmpty`
- HEAD errors return no body (`writeBody: false`)

## Handler Location

Handlers live as private static methods in `S3RequestRouter`, keeping route registration and dispatch co-located.
