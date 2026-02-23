# Mock S3 Kata

> Follow `openspec/constitution.md` — it defines non-negotiable rules for this project.

## Spec Implementation Order

Specs in `openspec/specs/` are organised by domain. Implement in this order — each depends on the ones above it.

| # | Domain spec | Status | Covers |
|---|-------------|--------|--------|
| 1 | `infrastructure` | Partial | Project structure complete; Docker/CI pending |
| 2 | `storage` | Complete | In-memory data layer |
| 3 | `server` | Complete | Error responses and HTTP routing |
| 4 | `buckets` | Pending | CreateBucket, DeleteBucket, HeadBucket, ListBuckets |
| 5 | `objects` | Pending | Object CRUD, batch delete, ListObjectsV1/V2 |
| 6 | `testing` | Pending | AWS SDK shims, SDK integration tests, rclone tests |
| 7 | `infrastructure` (remainder) | Pending | Dockerfile and GitHub Actions |

## Implementation Process

After implementing each spec, you SHOULD verify the changes before committing:

1. Run `dotnet build mock-s3.slnx` — build must succeed with 0 errors and 0 warnings
2. Run `dotnet test` — all tests must pass (or be skipped, e.g. rclone tests when rclone is absent)

If you cannot run a build or tests, say so explicitly and ask the user to verify before merging.

## OpenSpec Directory Structure

```
openspec/
├── specs/              # Source of truth — one spec.md per domain
│   ├── infrastructure/
│   ├── server/
│   ├── storage/
│   ├── buckets/
│   ├── objects/
│   └── testing/
└── constitution.md     # Non-negotiable project rules
```
