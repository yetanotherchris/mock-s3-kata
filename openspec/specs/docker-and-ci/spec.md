# Docker and CI/CD Specification

## Purpose

Define the Docker image build and GitHub Actions CI/CD pipeline for the mock S3 server.

## Requirements

### Requirement: Multi-Stage Dockerfile

The `Dockerfile` at the repository root SHALL use a multi-stage build.

**Build stage:**

- Base image: `mcr.microsoft.com/dotnet/sdk:10.0`
- Restores and publishes the `src/MockS3` project in Release configuration
- Uses `--no-restore` and layer caching for the restore step

**Runtime stage:**

- Base image: `mcr.microsoft.com/dotnet/aspnet:10.0`
- Copies only the published output from the build stage
- Sets `ASPNETCORE_URLS=http://+:9090`
- Exposes port `9090`
- Entry point: `dotnet MockS3.dll`

#### Scenario: Docker build produces a runnable image

- GIVEN the Dockerfile is present at the repository root
- WHEN `docker build -t mock-s3 .` is run
- THEN the image builds successfully without errors

#### Scenario: Server responds on port 9090

- GIVEN the image has been built
- WHEN `docker run -p 9090:9090 mock-s3` is started
- THEN the server accepts HTTP requests on port 9090

### Requirement: GitHub Actions Workflow

`.github/workflows/ci.yml` SHALL define a workflow triggered on:

- Push to `main`
- Pull requests targeting `main`

The workflow SHALL run on `ubuntu-latest` and contain the following steps in order:

1. Checkout the repository
2. Set up .NET 10 SDK
3. Restore dependencies (`dotnet restore`)
4. Build the solution (`dotnet build --no-restore -c Release`)
5. Run tests (`dotnet test --no-build -c Release`)
6. Build the Docker image
7. Push the Docker image to GitHub Container Registry (`ghcr.io`) — on push to `main` only

### Requirement: Docker Image Naming

The image SHALL be named `ghcr.io/${{ github.repository }}`.

It SHALL be tagged with:

- `latest` (on push to `main`)
- The short commit SHA (e.g. `ghcr.io/owner/repo:abc1234`)

### Requirement: Registry Authentication

The workflow SHALL authenticate to `ghcr.io` using the `GITHUB_TOKEN` secret provided by GitHub Actions.

#### Scenario: CI runs on pull request

- GIVEN a pull request is opened targeting `main`
- WHEN the CI workflow runs
- THEN build and test steps run
- THEN the Docker push step is skipped

#### Scenario: CI runs on push to main

- GIVEN a commit is pushed to `main`
- WHEN the CI workflow runs
- THEN all steps run including Docker build and push
- THEN the image is available at `ghcr.io/{owner}/{repo}:latest`
