# AWS SDK Compatibility — Proposal

## Why

The AWS SDK v4 for .NET has two incompatibilities with `TestServer` (in-process hosting):
1. It creates `Uri` objects with `DangerousDisablePathAndQueryCanonicalization=true`, which causes `TestServer.ClientHandler` to throw a `UriFormatException`.
2. It uses `aws-chunked` streaming encoding for PutObject, bypassing HTTP framing in-process so raw chunk-signed bytes reach the server.

## What

Add a `UriNormalizingHandler` delegating handler in the `MockS3.Tests.Spec` fixture layer that patches both issues before requests reach the server.

## Scope

Test fixture layer only — no changes to the production server code.
