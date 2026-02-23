## Introduction

This project is C#/.NET 10.0 in-memory mock AWS S3 server, written using OpenSpec and the AWS Smithy definitions. 

## Requirements

The project SHALL:

- Use OpenSpec for spec-driven development.
- Adhere to the requirements in the "dotnet-project-structure" skill.
- Adhere to the requirements in the "modern-csharp-coding-standards" skill.
- Use the latest version of packages e.g. "version=*"
- Write the specifications first, and this includes a spec for changes. The implementation will be explicitly prompted in a new session.
- Be an executable in-memory mock of AWS S3 written in C#/.NET 10.0. 
- Store S3 objects in memory, not on disk.
- Write all unit tests using the TUnit framework.
- Have a docker image as its artifact using a multi-stage build, exposing port 9090.
- Have a Github actions file that builds, tests and then creates a docker image stored in the Github registry.
- Have an Integration test project that tests using the AWS SDK v4 and WebApplicationFactory<Program> for in-process hosting (no network).
- Have an integration test project uses the rclone executable to test the mock S3 server.
- Check LSP diagnostics after each file edit and resolve all errors and warnings before committing.

## Resources

The following websites contain information for the requirements section:

- [OpenSpec](https://github.com/Fission-AI/OpenSpec) - Spec-driven development framework for AI coding assistants
- [AWS API Models](https://github.com/aws/aws-models) - Smithy model definitions for AWS services including S3
- [Smithy](https://smithy.io/) - Interface definition language used by AWS
- [modern-csharp-coding-standards skill](https://skillsmp.com/skills/aaronontheweb-dotnet-skills-skills-csharp-coding-standards-skill-md)
- [dotnet-project-structure skill](https://skillsmp.com/skills/aaronontheweb-dotnet-skills-skills-project-structure-skill-md)


## OpenSpec format

See: https://github.com/Fission-AI/OpenSpec/blob/main/docs/getting-started.md

You should use OpenSpec X as a tool rather than Claude Code for the specs

```
openspec/
├── specs/              # Source of truth — one spec.md per domain
│   ├── infrastructure/ # Project layout, build config, Docker, CI/CD
│   ├── server/         # Error responses, HTTP routing
│   ├── storage/        # In-memory storage layer
│   ├── buckets/        # Bucket operations (CreateBucket, DeleteBucket, etc.)
│   ├── objects/        # Object operations and listing
│   └── testing/        # SDK compatibility shims and integration tests
└── constitution.md     # Non-negotiable project rules
```

### Rules for this project

- **No `changes/` directory.** This is a greenfield project — there is no existing baseline to diff against. The `changes/` pattern from OpenSpec is for incremental modifications to a live system.
- **One spec.md per domain.** Do not split a domain into multiple spec files. If a domain covers several operations (e.g. objects covers PutObject, GetObject, listing, etc.), they all belong in the same `spec.md`.
- **Domains, not task categories.** Folder names reflect what the system *is* (`buckets`, `objects`), not how work was broken down (`bucket-operations`, `object-listing`).
- **`constitution.md`** defines non-negotiable project rules and must not be changed without explicit agreement.