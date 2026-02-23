# Rclone Integration Tests — Tasks

- [ ] Create `RcloneFixture` using `WebApplication.CreateBuilder()` + `StartAsync()`
- [ ] Create `RcloneRunner` with async stdout/stderr capture and timeout/kill
- [ ] Configure rclone via environment variables
- [ ] Write upload + list test (`rclone copy` + `rclone ls`)
- [ ] Write download test (`rclone copy` from bucket to temp dir)
- [ ] Add skip logic when rclone is not on PATH
- [ ] Verify build: `dotnet build mock-s3.slnx` passes with 0 warnings
- [ ] Verify tests: `dotnet test` passes (rclone tests skipped if absent)
