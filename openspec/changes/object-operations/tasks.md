# Object Operations — Tasks

- [ ] Implement `HandlePutObject` — aws-chunked decoding, store object, return ETag
- [ ] Implement `HandleGetObject` — return bytes + metadata headers
- [ ] Implement `HandleHeadObject` — return headers only
- [ ] Implement `HandleDeleteObject` — idempotent 204
- [ ] Implement `HandleCopyObject` — copy via x-amz-copy-source header
- [ ] Implement `HandleDeleteObjects` — parse XML body, batch delete
- [ ] Write unit tests for each handler
- [ ] Verify build: `dotnet build mock-s3.slnx` passes with 0 warnings
- [ ] Verify tests: `dotnet test` passes
