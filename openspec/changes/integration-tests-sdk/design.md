# AWS SDK Integration Tests — Design

## Fixture

`MockS3Fixture` creates one `WebApplicationFactory<Program>` and one `AmazonS3Client` per test session. Configured with fake credentials, `ForcePathStyle=true`, and `UriNormalizingHandler` in the HTTP pipeline.

## Bucket Isolation

Each test class creates a uniquely named bucket at the start and deletes it (with all objects) at the end using a TUnit `[After(Test)]` cleanup hook.

## SDK v4 Quirks

- `ListObjectsV2` returns `null` for `S3Objects` on empty buckets (not an empty list). Tests use `response.S3Objects ?? []`.
- `ServiceURL` must end with `/` for the SDK to resolve bucket URLs correctly in path-style mode.
