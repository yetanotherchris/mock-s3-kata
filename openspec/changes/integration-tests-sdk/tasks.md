# AWS SDK Integration Tests — Tasks

- [ ] Create `MockS3Fixture` with `WebApplicationFactory` + `AmazonS3Client`
- [ ] Write CreateBucket tests
- [ ] Write DeleteBucket tests
- [ ] Write HeadBucket tests (found + not found)
- [ ] Write ListBuckets test
- [ ] Write PutObject tests (content, content-type, user metadata)
- [ ] Write GetObject round-trip test
- [ ] Write HeadObject tests (found + not found)
- [ ] Write DeleteObject test
- [ ] Write CopyObject test
- [ ] Write DeleteObjects (batch) test
- [ ] Write ListObjectsV2 tests (empty, with objects, with prefix, with delimiter, paginated)
- [ ] Verify build: `dotnet build mock-s3.slnx` passes with 0 warnings
- [ ] Verify tests: `dotnet test` passes
