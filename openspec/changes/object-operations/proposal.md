# Object Operations — Proposal

## Why

Bucket operations are implemented. The next layer is object CRUD: PutObject, GetObject, HeadObject, DeleteObject, CopyObject, and batch DeleteObjects.

## What

Implement all six object operation handlers in `S3RequestRouter`, backed by `InMemoryS3Storage`. CopyObject and PutObject share the same route and are distinguished by the `x-amz-copy-source` header.

## Scope

- PutObject — store bytes, return ETag
- GetObject — return stored bytes + metadata headers
- HeadObject — return headers only, no body
- DeleteObject — idempotent delete
- CopyObject — copy between keys/buckets via header
- DeleteObjects — batch XML delete, POST ?delete
