# AWS SDK Integration Tests Specification

## Purpose

Define the integration test project (`MockS3.Tests.Spec`) that validates the mock S3 server using the real AWS SDK v4 for .NET, hosted in-process via `WebApplicationFactory<Program>`.

## Requirements

### Requirement: Project Setup

`tests/MockS3.Tests.Spec` SHALL be a TUnit test project targeting `net10.0`.

It SHALL reference:

- `MockS3` (project reference)
- `AWSSDK.S3` NuGet package
- `Microsoft.AspNetCore.Mvc.Testing` NuGet package
- TUnit test framework packages

### Requirement: MockS3Fixture

A shared `MockS3Fixture` class SHALL manage the lifecycle of the in-process test host and the configured S3 client.

`MockS3Fixture` SHALL:

- Create a `WebApplicationFactory<Program>` instance
- Create an `AmazonS3Client` configured with:
  - Fake credentials (any non-empty access key and secret)
  - `ServiceURL` pointing to the in-process server
  - `ForcePathStyle = true`
  - The `UriNormalizingHandler` delegating handler inserted into the HTTP pipeline
- Be shared across all tests in the session (instantiated once)

### Requirement: Per-Test Bucket Isolation

Each test class (or test) SHALL operate on its own uniquely named bucket to avoid cross-test interference.

A bucket SHALL be created at the start of each test and deleted (along with all its objects) at the end.

#### Scenario: Isolated test execution

- GIVEN two tests run concurrently
- WHEN each test creates objects in its own bucket
- THEN neither test observes the other's objects

### Requirement: Test Coverage

The test project SHALL include tests for:

- CreateBucket
- DeleteBucket
- HeadBucket (found and not found)
- ListBuckets
- PutObject (with content, content-type, and user metadata)
- GetObject (round-trip with PutObject)
- HeadObject (found and not found)
- DeleteObject
- CopyObject
- DeleteObjects (batch)
- ListObjectsV2 (empty bucket, with objects, with prefix, with delimiter, paginated)

#### Scenario: PutObject and GetObject round-trip

- GIVEN the fixture is running
- WHEN a test puts an object with known byte content
- THEN GetObject returns the same bytes
- THEN the ETag in the GetObject response matches the ETag from PutObject

#### Scenario: ListObjectsV2 with empty bucket

- GIVEN a freshly created bucket contains no objects
- WHEN `ListObjectsV2` is called via the SDK
- THEN `response.S3Objects` is null or empty (AWS SDK v4 returns `null` for empty, unlike v3)
- THEN `response.KeyCount` is 0

#### Scenario: ListObjectsV2 with prefix and delimiter

- GIVEN a bucket contains `images/a.png`, `images/b.png`, and `docs/readme.txt`
- WHEN `ListObjectsV2` is called with `Prefix = "images/"` and `Delimiter = "/"`
- THEN the response contains the two image keys
- THEN `docs/readme.txt` is not present

### Requirement: No Real AWS Credentials

The test fixture SHALL use fake credentials and SHALL NOT require real AWS credentials or network access to AWS.
