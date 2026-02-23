# Infrastructure Specification

## Purpose

Define the repository layout, build configuration, and CI/CD infrastructure for the mock S3 server solution.

---

## Project Structure

### Requirement: Solution File

The solution SHALL use the `.slnx` XML format (modern .NET solution file introduced in .NET 9).

#### Scenario: Solution contains all projects

- GIVEN the `.slnx` solution file exists at the repository root
- WHEN opened by an IDE or built with `dotnet build`
- THEN all `src/` and `tests/` projects are discoverable under organized virtual folders (`/src/`, `/tests/`, `/build/`)

### Requirement: Repository Layout

The repository SHALL use the following structure:

```
mock-s3-kata/
├── .github/
│   └── workflows/
│       └── ci.yml
├── openspec/
│   └── specs/
├── src/
│   └── MockS3/
│       └── MockS3.csproj
├── tests/
│   ├── MockS3.Tests.Unit/
│   │   └── MockS3.Tests.Unit.csproj
│   ├── MockS3.Tests.Spec/
│   │   └── MockS3.Tests.Spec.csproj
│   └── MockS3.Tests.Rclone/
│       └── MockS3.Tests.Rclone.csproj
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── NuGet.Config
├── Dockerfile
├── mock-s3.slnx
└── RELEASE_NOTES.md
```

### Requirement: Centralized Build Configuration

`Directory.Build.props` at the repository root SHALL apply to all projects and include:

- `<LangVersion>latest</LangVersion>`
- `<Nullable>enable</Nullable>`
- `<ImplicitUsings>enable</ImplicitUsings>`
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- A reusable `<NetAppVersion>net10.0</NetAppVersion>` property

#### Scenario: Nullable warnings are errors

- GIVEN `TreatWarningsAsErrors` and `Nullable` are enabled
- WHEN code introduces a nullable dereference warning
- THEN the build fails

### Requirement: Central Package Management

`Directory.Packages.props` SHALL manage all NuGet package versions centrally with `ManagePackageVersionsCentrally=true`.

All package versions SHALL use `Version="*"` to reference the latest available version.

Individual project `.csproj` files SHALL omit version attributes from `<PackageReference>` elements.

### Requirement: SDK Pinning

`global.json` SHALL pin the .NET SDK to version `10.0.100` with `rollForward: "latestFeature"`.

### Requirement: Source Project

`src/MockS3/MockS3.csproj` SHALL be an executable (`<OutputType>Exe</OutputType>`) targeting `net10.0`.

### Requirement: Test Projects

Each test project SHALL target `net10.0` and reference TUnit packages.

`MockS3.Tests.Spec` and `MockS3.Tests.Rclone` SHALL reference `src/MockS3` as a project reference.

`MockS3.Tests.Unit` SHALL reference `src/MockS3` as a project reference.

---

## Docker and CI/CD

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
