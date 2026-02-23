# HTTP Routing — Tasks

- [x] Create `src/MockS3/Routing/S3RequestRouter.cs` with route registration
- [x] Handle CopyObject detection (`x-amz-copy-source` header)
- [x] Handle DeleteObjects detection (`?delete` query key, return 405 if absent)
- [x] Implement `GetNonEmptyQueryParam` helper
- [x] Update `Program.cs` to call `S3RequestRouter.Register`
- [x] Write unit tests for `GetNonEmptyQueryParam`
- [x] Verify build: `dotnet build mock-s3.slnx` passes with 0 warnings
- [x] Verify tests: `dotnet test` passes
