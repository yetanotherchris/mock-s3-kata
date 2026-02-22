# Project Structure Specification

## Purpose

Define the repository layout, build configuration, packaging, and CI/CD infrastructure for the mock S3 server solution.

## Requirements

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
