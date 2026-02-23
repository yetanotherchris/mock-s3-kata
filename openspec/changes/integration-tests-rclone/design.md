# Rclone Integration Tests — Design

## RcloneFixture

Uses `WebApplication.CreateBuilder()` + `app.StartAsync()` directly (not `WebApplicationFactory`) to bind a real TCP port. Port is read from `app.Urls` after startup.

## RcloneRunner

Starts rclone as a subprocess, captures stdout/stderr asynchronously, applies a 30-second timeout. On timeout: kill the entire process tree first, then await I/O tasks, then throw `TimeoutException`.

## Rclone Configuration

Environment variables: `type=s3`, `provider=Other`, `access_key_id=test`, `secret_access_key=test`, `endpoint=http://localhost:{port}`, `path_style=true`.

## V1 Listing Behaviour

rclone with `provider=Other` uses ListObjects V1 for all listings. It sends `delimiter=` (empty string) rather than omitting the parameter. The server's `GetNonEmptyQueryParam` helper handles this correctly.
