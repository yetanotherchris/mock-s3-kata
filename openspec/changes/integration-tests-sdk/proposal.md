# AWS SDK Integration Tests — Proposal

## Why

The mock server needs end-to-end validation using the real AWS SDK v4 to confirm it behaves like genuine S3 from a client's perspective.

## What

Build out `MockS3.Tests.Spec` with tests for all major operations using `AmazonS3Client` against the in-process server via `WebApplicationFactory`. Tests run without real AWS credentials or network access.

## Scope

All operations: bucket CRUD, object CRUD, listing (V2), batch delete. Each test operates on its own isolated bucket.
