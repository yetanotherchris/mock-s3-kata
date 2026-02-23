# Project Constitution

Non-negotiable rules for this project. These must not be changed without explicit agreement.

## Runtime and Language

- Target: .NET 10, C# (LangVersion: latest)
- Server: ASP.NET Core minimal API
- All data stored in memory only — no disk writes, no persistence across restarts

## Project Structure

- Solution file: `mock-s3.slnx` (modern XML format)
- Source: `src/MockS3/`
- Tests: `tests/MockS3.Tests.Unit/`, `tests/MockS3.Tests.Spec/`, `tests/MockS3.Tests.Rclone/`
- Centralised build config: `Directory.Build.props` at root
- Centralised package versions: `Directory.Packages.props` at root

## Build Requirements

After every change:

1. `dotnet build mock-s3.slnx` must succeed with **0 errors and 0 warnings**
2. `dotnet test` must pass with **0 failures**

`TreatWarningsAsErrors` is enabled globally and must remain so.

LSP diagnostics must be checked after each file edit — all errors and warnings must be resolved before committing.

## Package Management

- All NuGet versions use `Version="*"` (latest, via central package management)
- `ManagePackageVersionsCentrally=true` must remain set
- No version attributes in individual `.csproj` files

## Testing

- Test framework: TUnit only
- `MockS3.Tests.Unit`: pure unit tests (no HTTP, no network)
- `MockS3.Tests.Spec`: AWS SDK v4 integration tests via `WebApplicationFactory` (in-process)
- `MockS3.Tests.Rclone`: rclone integration tests via real Kestrel port — skipped if rclone is absent

## S3 API

- Path-style addressing only (`/{bucket}/{key}`) — no virtual-hosted-style
- AWS XML error format for all non-HEAD error responses
- Port 9090 for the Docker image

## Docker

- Multi-stage Dockerfile: SDK image for build, ASP.NET runtime image for output
- `ASPNETCORE_URLS=http://+:9090`
- Exposes port 9090

## Commit and Branch Discipline

- Always create a branch before starting work
- Commit to the branch with meaningful messages
- Do not commit if build or tests fail
