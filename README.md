# FamilyDose

FamilyDose is a self-hosted, mobile-first family medication tracking application.

## Goals

- Track medications for multiple children.
- Record medication administrations.
- Calculate next eligible administration times.
- Support scheduled and as-needed medications.
- Enforce user-configured minimum intervals and rolling 24-hour limits.
- Provide optional medication reminders.
- Support QR-based device pairing without traditional user accounts.
- Maintain auditable administration history.

## Technology

- ASP.NET Core (.NET 10)
- SvelteKit and TypeScript
- SQLite
- Docker
- Progressive Web App (PWA)

## Development

Development occurs on LinuxPlex.

Production deployment will use TrueNAS SCALE custom Docker Compose applications.

## Medical Disclaimer

FamilyDose is a medication tracking tool, not a source of medical advice.

Always confirm medication selection, dosage, frequency, and age appropriateness with a healthcare professional.

FamilyDose does not verify whether a medication or dose is safe or appropriate.
