# EMS module instructions

## Canonical EMS documents
Read these first:
- ../docs/specs/EMS/current/Phase-01-spec-ems-v1.4.md
- ../docs/specs/EMS/current/Phase-01-architecture-ems-v1.4.md
- ../docs/specs/EMS/current/Phase-01-erd-ems-v1.4.md
- ../docs/specs/EMS/current/Phase-01-roles-matrix-ems-v1.4.md
- ../docs/specs/EMS/current/Phase-01-security-session-ems-v1.4.md
- ../docs/specs/EMS/current/Phase-01-api-contracts-ems-v1.4.md
- ../docs/specs/EMS/current/Phase-01-codex-ingestion-rules-ems-v1.4.md
- ../docs/specs/EMS/current/Phase-01-audit-taxonomy-matrix-ems-v1.4.md

## EMS rules
- Backend target: .NET 10
- Auth model: cookie auth with single active session
- Role changes are admin-only and revoke active sessions
- Date-only fields must not be timezone-shifted
- UTC date-times must use ISO 8601 with `Z`
- Employee addresses support multiple records with one primary
- All implementation code lives under `EMS/`
- Container and runtime files live under `EMS/deploy/`
