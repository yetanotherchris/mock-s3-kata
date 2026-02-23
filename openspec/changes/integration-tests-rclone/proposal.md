# Rclone Integration Tests — Proposal

## Why

The AWS SDK tests validate in-process behaviour. Rclone validates the server from the perspective of a real HTTP client using its own S3 dialect (ListObjects V1, empty delimiter, path-style).

## What

Build out `MockS3.Tests.Rclone` to start the server on a real Kestrel port and execute rclone commands against it. Tests are skipped if rclone is not on PATH.

## Scope

- Real Kestrel server on `localhost:0` (OS-assigned port)
- rclone copy (upload), ls (list), copy (download)
- Uses `provider=Other` which forces ListObjects V1
