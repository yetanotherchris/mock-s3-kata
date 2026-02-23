# HTTP Routing — Proposal

## Why

Storage and error formatting are in place but all requests return 404 — there is no routing layer. The server needs a dispatch table that maps incoming HTTP method + path + query/header signals to the correct S3 operation.

## What

Add an `S3RequestRouter` that registers all route patterns with ASP.NET Core's minimal API routing and dispatches each request to the correct handler method. Operation logic is left as stubs (501) until the bucket-operations and object-operations specs are implemented.

## Scope

- Route registration for all 11 S3 operations
- CopyObject detection via `x-amz-copy-source` header
- DeleteObjects detection via `?delete` query key
- `GetNonEmptyQueryParam` helper — empty string values treated as absent
- Unmatched method on known path → 405
