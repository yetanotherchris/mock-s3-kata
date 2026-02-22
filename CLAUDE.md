# Mock S3 Kata

## Spec Implementation Order

Specs in `openspec/specs/` should be implemented in this order. Each depends on the ones above it.

| # | Spec | Status | Notes |
|---|------|--------|-------|
| 1 | `project-structure` | Complete | Solution, csproj files, build config |
| 2 | `in-memory-storage` | Pending | Core data layer, must precede all handlers |
| 3 | `error-responses` | Pending | AWS XML error format, needed by every handler |
| 4 | `http-routing` | Pending | Request dispatch, depends on storage and error format |
| 5 | `bucket-operations` | Pending | CreateBucket, DeleteBucket, HeadBucket, ListBuckets |
| 6 | `object-operations` | Pending | PutObject, GetObject, HeadObject, DeleteObject, CopyObject, DeleteObjects |
| 7 | `object-listing` | Pending | ListObjectsV1, ListObjectsV2 with prefix/delimiter/pagination |
| 8 | `aws-sdk-compatibility` | Pending | Test-layer shims for in-process SDK testing |
| 9 | `integration-tests-sdk` | Pending | AWS SDK v4 integration tests (MockS3.Tests.Spec) |
| 10 | `integration-tests-rclone` | Pending | Rclone integration tests (MockS3.Tests.Rclone) |
| 11 | `docker-and-ci` | Pending | Dockerfile and GitHub Actions workflow |

## Implementation Process

After implementing each spec, you SHOULD verify the changes before committing:

1. Run `dotnet build mock-s3.slnx` — build must succeed with 0 errors and 0 warnings
2. Run `dotnet test` — all tests must pass (or be skipped, e.g. rclone tests when rclone is absent)

If you cannot run a build or tests, say so explicitly and ask the user to verify before merging.

## OpenSpec Format Notes

- `aws-sdk-compatibility/spec.md` reads more like a design doc — it names internal classes and handler chains. It should ideally be a `design.md`. When implementing, the observable behavior contract is: in-process SDK tests must work without real AWS credentials.
- `in-memory-storage/spec.md` references concrete types (`ConcurrentDictionary`) which are implementation choices rather than behavioral specs.
