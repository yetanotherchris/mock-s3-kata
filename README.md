# Mock S3 (Kata)

An in-memory mock S3 server for local integration testing, written in C#.

**Supported operations:** CreateBucket, DeleteBucket, HeadBucket, ListBuckets, PutObject, GetObject, HeadObject, DeleteObject, CopyObject, DeleteObjects (batch), ListObjectsV1, ListObjectsV2

## Run

```bash
docker build -t mock-s3 .
docker run -p 9090:9090 mock-s3
```

Or without Docker:

```bash
dotnet run --project src/MockS3
```

The server listens on port 9090 by default. Point your AWS SDK at `http://localhost:9090` with path-style addressing and any credentials.

## Development

Uses [OpenSpec](openspec/constitution.md) — requirements live in `openspec/specs/` and the constitution defines non-negotiable rules.

```bash
dotnet build mock-s3.slnx
dotnet test
```

Rclone integration tests are skipped automatically if rclone is not on PATH.
