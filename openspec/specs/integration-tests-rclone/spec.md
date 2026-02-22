# Rclone Integration Tests Specification

## Purpose

Define the integration test project (`MockS3.Tests.Rclone`) that validates the mock S3 server using the rclone command-line tool against a real Kestrel HTTP listener.

## Requirements

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
