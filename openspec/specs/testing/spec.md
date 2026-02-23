# Testing Specification

## Purpose

Define the integration test projects and the compatibility shims required to test the mock S3 server using the AWS SDK v4 and rclone.

---

## AWS SDK Compatibility

### Requirement: URI Normalization Handler

The test fixture SHALL include a `DelegatingHandler` named `UriNormalizingHandler` that re-creates request URIs from their `OriginalString` before forwarding.

**Background:** AWS SDK v4 creates `Uri` objects using `UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = true }`. The ASP.NET Core `TestServer.ClientHandler` calls `uri.GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped)` internally, which throws a `UriFormatException` on such URIs.

**Fix:** Before forwarding the request, reconstruct the URI using:

```csharp
request.RequestUri = new Uri(request.RequestUri!.OriginalString);
```

#### Scenario: SDK sends a request with a non-canonical URI

- GIVEN the AWS SDK creates a `Uri` with disabled path canonicalization
- WHEN `PutObject` or any SDK operation is called via the in-process test client
- THEN the `UriNormalizingHandler` re-creates the URI from `OriginalString`
- THEN the request reaches the ASP.NET Core pipeline without throwing

### Requirement: aws-chunked Body Decoding

The `UriNormalizingHandler` (or a companion handler) SHALL detect and decode `aws-chunked` encoded request bodies before they reach the server.

**Background:** AWS SDK v4 uses `STREAMING-AWS4-HMAC-SHA256-PAYLOAD-TRAILER` transfer encoding by default for PutObject. The in-process `TestServer` bypasses HTTP framing, so the raw chunk-signed bytes reach the server instead of the decoded content.

**Detection:** A request body is aws-chunked if either:

- The `x-amz-content-sha256` header value starts with `STREAMING-`
- OR the `x-amz-decoded-content-length` request header is present

**Decoding:** Each chunk has the format:

```
<hex-size>[;chunk-signature=...]\r\n
<chunk-data>\r\n
```

The handler SHALL read and decode all chunks until a zero-length chunk is reached, then replace the request body with the decoded bytes.

#### Scenario: SDK sends aws-chunked PutObject

- GIVEN AWS SDK v4 sends a PutObject request with `x-amz-content-sha256: STREAMING-AWS4-HMAC-SHA256-PAYLOAD-TRAILER`
- WHEN the handler processes the request
- THEN the raw chunked body is decoded to the original content bytes
- THEN the server receives and stores the correct content

### Requirement: Handler Chain

The `UriNormalizingHandler` SHALL be inserted into the `HttpClient` handler chain between the `AmazonS3Client` and the `TestServer.ClientHandler`:

```
AmazonS3Client → UriNormalizingHandler → TestServer.ClientHandler
```

### Requirement: Scope

These handlers apply only to `MockS3.Tests.Spec`. The `MockS3.Tests.Rclone` project does not require them (rclone connects via a real TCP socket, so normal HTTP framing applies).

---

## AWS SDK Integration Tests

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

---

## Rclone Integration Tests

### Requirement: Project Setup

`tests/MockS3.Tests.Rclone` SHALL be a TUnit test project targeting `net10.0`.

It SHALL reference `MockS3` as a project reference.

### Requirement: Real Kestrel Server

The `RcloneFixture` SHALL start the mock S3 server using `WebApplication.CreateBuilder()` and `app.StartAsync()` directly, NOT using `WebApplicationFactory`.

**Background:** `WebApplicationFactory` uses an in-process `TestServer` which does not bind a real TCP port. Rclone requires a real network port to connect to.

The server SHALL bind to `http://localhost:0` (OS-assigned random port). The actual port SHALL be read from `app.Urls` after startup.

#### Scenario: Server starts on available port

- GIVEN the fixture starts the application
- WHEN `app.StartAsync()` completes
- THEN `app.Urls` contains the bound address including the assigned port
- THEN the server accepts TCP connections on that port

### Requirement: RcloneFixture

`RcloneFixture` SHALL:

- Start the mock S3 server on a random port
- Expose the server base URL to tests
- Stop the server after all tests in the session complete
- Be shared across all tests (instantiated once per session)

### Requirement: RcloneRunner

A `RcloneRunner` class SHALL execute rclone as a subprocess and capture its output.

`RcloneRunner` SHALL:

- Accept a list of rclone arguments
- Start the rclone process and capture stdout and stderr asynchronously
- Apply a configurable timeout (default: 30 seconds)
- On timeout: kill the process tree before awaiting the stdout/stderr tasks, then throw `TimeoutException`
- Return a result containing: exit code, stdout (string), stderr (string)

#### Scenario: Rclone command succeeds

- GIVEN rclone is installed and reachable via PATH
- WHEN `RcloneRunner` executes `rclone ls :s3:my-bucket`
- THEN the exit code is `0` and stdout contains the listing

#### Scenario: Process timeout

- GIVEN a rclone command hangs indefinitely
- WHEN the timeout expires
- THEN the process tree is killed
- THEN `RcloneRunner` throws `TimeoutException`

### Requirement: Rclone S3 Configuration

The fixture SHALL configure rclone to connect to the mock server using environment variables or a config file with the following settings:

- `type = s3`
- `provider = Other` (avoids AWS-specific behavior in rclone)
- `access_key_id = test`
- `secret_access_key = test`
- `endpoint = http://localhost:{port}`
- `path_style = true`

**Note:** rclone with `provider=Other` uses ListObjects V1 (not V2) for all listing operations. It sends `delimiter=` (empty string) when no delimiter is desired rather than omitting the parameter.

### Requirement: Test Coverage

Tests SHALL cover at minimum:

- Copy a local file to the mock S3 bucket using `rclone copy`
- List objects in the bucket using `rclone ls`
- Copy an object from the mock S3 bucket to a local path using `rclone copy`

#### Scenario: Upload and list

- GIVEN a bucket exists and rclone is configured to point to the mock server
- WHEN `rclone copy <local-file> :s3:my-bucket` is executed
- THEN the exit code is `0`
- WHEN `rclone ls :s3:my-bucket` is executed
- THEN the output includes the uploaded filename

### Requirement: Test Skip When Rclone Absent

If the rclone executable is not found on PATH, tests SHALL be skipped (not failed).
