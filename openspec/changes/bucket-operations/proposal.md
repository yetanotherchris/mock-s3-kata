# Bucket Operations — Proposal

## Why

HTTP routing is in place but all route handlers return 501. The bucket-level operations (CreateBucket, DeleteBucket, HeadBucket, ListBuckets) are the first layer of real behaviour that can be tested end-to-end via the AWS SDK.

## What

Implement the four bucket operation handlers inside `S3RequestRouter`, backed by `InMemoryS3Storage`. Each handler follows the AWS S3 REST semantics defined in `specs/bucket-operations/spec.md`.

## Scope

- `HandleCreateBucket` — idempotent PUT, returns 200 + Location header
- `HandleDeleteBucket` — DELETE with NoSuchBucket / BucketNotEmpty guards
- `HandleHeadBucket` — HEAD with 200/404, no body
- `HandleListBuckets` — GET / returning AWS ListAllMyBucketsResult XML
