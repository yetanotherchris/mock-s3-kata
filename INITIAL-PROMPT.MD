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

## Resources

The following websites contain information for the requirements section:

- [OpenSpec](https://github.com/Fission-AI/OpenSpec) - Spec-driven development framework for AI coding assistants
- [AWS API Models](https://github.com/aws/aws-models) - Smithy model definitions for AWS services including S3
- [Smithy](https://smithy.io/) - Interface definition language used by AWS
- [modern-csharp-coding-standards skill](https://skillsmp.com/skills/aaronontheweb-dotnet-skills-skills-csharp-coding-standards-skill-md)
- [dotnet-project-structure skill](https://skillsmp.com/skills/aaronontheweb-dotnet-skills-skills-project-structure-skill-md)