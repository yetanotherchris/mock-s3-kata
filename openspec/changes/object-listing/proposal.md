# Object Listing — Proposal

## Why

Object CRUD is in place. Listing (ListObjectsV1 and ListObjectsV2) is required for clients like rclone and the AWS SDK to enumerate bucket contents.

## What

Implement `HandleListObjects` to branch between V1 (no `list-type` param) and V2 (`list-type=2`). Both support prefix filtering, delimiter grouping, and max-keys pagination. V1 uses `marker`, V2 uses `continuation-token`.

## Scope

- ListObjectsV1 with prefix, delimiter, max-keys, marker
- ListObjectsV2 with prefix, delimiter, max-keys, continuation-token
- Empty string params treated as absent (rclone sends `delimiter=`)
