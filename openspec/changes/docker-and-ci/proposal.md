# Docker and CI/CD — Proposal

## Why

The server needs a distributable artifact (Docker image) and automated validation on every push/PR via GitHub Actions.

## What

Add a multi-stage Dockerfile that builds and packages the server, and a GitHub Actions workflow that builds, tests, and pushes the image to GitHub Container Registry.

## Scope

- Dockerfile at repo root, port 9090, multi-stage (SDK + ASP.NET runtime images)
- `.github/workflows/ci.yml` triggered on push to `main` and PRs targeting `main`
- Docker push to `ghcr.io` on push to `main` only
