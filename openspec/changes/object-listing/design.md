# Object Listing — Design

## Branching

`HandleListObjects` reads the `list-type` query param via `GetNonEmptyQueryParam`. Value `2` → V2 path, anything else → V1 path.

## Prefix Filtering

Filter bucket objects to those whose key starts with the (non-empty) prefix string.

## Delimiter Grouping

Keys containing the delimiter character after the prefix are collapsed into `CommonPrefixes`. Only the portion up to and including the first delimiter occurrence is included. An empty string delimiter is treated as absent (no grouping).

## Pagination

V1: `marker` skips keys lexicographically ≤ marker value. V2: `continuation-token` is a Base64-encoded marker key; `NextContinuationToken` is returned when truncated.

## Empty Param Guard

Both handlers call `GetNonEmptyQueryParam` for `delimiter` and `prefix` so empty-string values from rclone are treated as absent.

## XML Formats

V1: `ListBucketResult`. V2: same structure plus `KeyCount` and `NextContinuationToken`, `ContinuationToken` replaces `Marker`.
