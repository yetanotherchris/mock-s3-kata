# Bucket Operations — Tasks

- [ ] Implement `HandleListBuckets` — returns ListAllMyBucketsResult XML
- [ ] Implement `HandleCreateBucket` — idempotent, returns 200 + Location header
- [ ] Implement `HandleDeleteBucket` — NoSuchBucket / BucketNotEmpty guards
- [ ] Implement `HandleHeadBucket` — 200/404, no body
- [ ] Write unit tests for each handler
- [ ] Verify build: `dotnet build mock-s3.slnx` passes with 0 warnings
- [ ] Verify tests: `dotnet test` passes
