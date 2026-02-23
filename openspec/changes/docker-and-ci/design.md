# Docker and CI/CD — Design

## Dockerfile

Two stages:
1. `build` — base `mcr.microsoft.com/dotnet/sdk:10.0`, restores then publishes `src/MockS3` in Release configuration with layer caching for the restore step
2. `runtime` — base `mcr.microsoft.com/dotnet/aspnet:10.0`, copies publish output, sets `ASPNETCORE_URLS=http://+:9090`, exposes port 9090, entry point `dotnet MockS3.dll`

## GitHub Actions

Workflow on `ubuntu-latest`:
1. Checkout
2. Setup .NET 10 SDK
3. `dotnet restore`
4. `dotnet build --no-restore -c Release`
5. `dotnet test --no-build -c Release`
6. Docker build
7. Docker push to `ghcr.io` (push to `main` only)

Image tagged `latest` and short commit SHA. Authenticates with `GITHUB_TOKEN`.
