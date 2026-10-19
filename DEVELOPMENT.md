# FamilyDose Development

## Development Environment

Host: LinuxPlex
Operating System: Ubuntu 24.04 LTS
Development directory: /home/linuxplex/development/FamilyDose

## Production Environment

Host: WescheHexNAS
Platform: TrueNAS SCALE
Deployment: Custom Docker Compose application
Reverse proxy: Nginx Proxy Manager

## Architecture

Backend: ASP.NET Core .NET 10
Frontend: SvelteKit / TypeScript
Database: SQLite
Packaging: Docker
Source control: GitHub
CI/CD: GitHub Actions / GHCR

## Development Principles

- Use temporary Docker SDK containers when practical.
- Keep production and development environments separate.
- Write automated tests for business logic.
- Use deterministic clocks for scheduling tests.
- Preserve medication administration history.
- Use UTC timestamps internally.
- Validate all user input.
- Protect authentication credentials.
- Verify builds and tests before releases.

## Current Status

Milestone 1: Initial project scaffolding.

## Future Development

1. Child profiles
2. Medication configuration
3. Administration tracking
4. Scheduling engine
5. QR device pairing
6. Push notifications
7. PWA installation
8. Production deployment
