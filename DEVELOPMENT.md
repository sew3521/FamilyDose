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

## Frontend Notes

The frontend was scaffolded using SvelteKit 3 and TypeScript.

The generated project uses vite.config.ts.

The initial production build uses adapter-auto. A suitable
production adapter will be configured during Docker integration.

## Dedication Easter Egg

The About screen will contain a hidden dedication to Callen
and Graham Wesche.

Tapping the application version number five times will reveal
the dedication.

## Code Readability and Maintainability

FamilyDose prioritizes straightforward, human-readable code.

### General Principles

- Prefer simple implementations over unnecessary abstractions.
- Use descriptive class, method, and variable names.
- Keep methods focused on one responsibility.
- Keep related functionality together.
- Avoid unnecessary interfaces and generic frameworks.
- Handle errors explicitly with meaningful messages.
- Prefer maintainability over clever programming tricks.

### Comments

Explain why code exists and document non-obvious behavior.

Do not add comments that simply repeat what the code does.

Medication scheduling calculations require particularly clear
documentation because correctness is essential.

### Testing

- Use descriptive test names.
- Test actual application behavior whenever practical.
- Use isolated SQLite databases for integration tests.
- Keep tests deterministic with injectable time.
- Test validation failures and boundary conditions.
- Add regression tests when fixing defects.

### Architecture

Do not introduce complexity without a demonstrated benefit.

Prefer readable, direct implementations over excessive
abstraction layers.
