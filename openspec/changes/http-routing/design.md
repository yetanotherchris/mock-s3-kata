# HTTP Routing — Design

## Route Registration

`S3RequestRouter.Register(WebApplication app)` is called from `Program.cs` after building the app. It uses `MapGet`, `MapPut`, `MapDelete`, `MapPost`, and `MapMethods` to register all routes.

## Route Patterns

| Pattern | Method(s) | Dispatch |
|---------|-----------|----------|
| `/` | GET | ListBuckets |
| `/{bucket}` | PUT | CreateBucket |
| `/{bucket}` | DELETE | DeleteBucket |
| `/{bucket}` | HEAD | HeadBucket |
| `/{bucket}` | GET | ListObjectsV1 or V2 (check `list-type` param) |
| `/{bucket}` | POST | DeleteObjects (require `?delete`; else 405) |
| `/{bucket}/{**key}` | PUT | PutObject or CopyObject (check `x-amz-copy-source` header) |
| `/{bucket}/{**key}` | GET | GetObject |
| `/{bucket}/{**key}` | DELETE | DeleteObject |
| `/{bucket}/{**key}` | HEAD | HeadObject |

`{**key}` is a catch-all that captures multi-segment keys including slashes.

## Empty Query Param Handling

Static helper `GetNonEmptyQueryParam(IQueryCollection, string)` returns `null` when the value is an empty string, so `delimiter=` and `prefix=` are treated as absent.

## Stub Handlers

All handlers return `Results.StatusCode(501)` until filled in by subsequent specs. `HandlePostBucket` returns 405 immediately if `?delete` is absent.
