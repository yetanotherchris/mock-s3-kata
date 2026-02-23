# AWS SDK Compatibility — Tasks

- [ ] Create `UriNormalizingHandler` with URI reconstruction
- [ ] Add aws-chunked body decoding to `UriNormalizingHandler`
- [ ] Create `MockS3Fixture` using `WebApplicationFactory` + configured `AmazonS3Client`
- [ ] Wire `UriNormalizingHandler` into the `AmazonS3Client` HTTP pipeline
- [ ] Verify build: `dotnet build mock-s3.slnx` passes with 0 warnings
- [ ] Verify tests: `dotnet test` passes
