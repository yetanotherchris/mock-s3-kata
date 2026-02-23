# Object Listing — Tasks

- [ ] Implement ListObjectsV1 with full XML response
- [ ] Implement ListObjectsV2 with full XML response
- [ ] Add prefix filtering to both
- [ ] Add delimiter grouping (CommonPrefixes) to both
- [ ] Add max-keys pagination to both (default 1000, clamped to 1000)
- [ ] Add V1 marker support
- [ ] Add V2 continuation-token support
- [ ] Write unit tests for prefix/delimiter/pagination logic
- [ ] Verify build: `dotnet build mock-s3.slnx` passes with 0 warnings
- [ ] Verify tests: `dotnet test` passes
