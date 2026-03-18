# EMS Implementation Notes

## Current checkpoint status

The current EMS foundation checkpoint includes:
- backend solution skeleton under `EMS/`
- Docker-based local runtime baseline
- cookie authentication with persisted user sessions
- employee read/list endpoints
- Wolverine integrated as the API-to-application boundary for the implemented auth and employee read flows
- central package management, shared build props, and `*.slnx`
- local secret handling through `EMS/.env` with committed placeholders in `EMS/.env.example`

## Verified at this checkpoint

- solution restore succeeds
- solution build succeeds
- current tests pass

## Known gaps against the approved Phase 1 architecture/plan

The following items are still pending and should be treated as known implementation gaps, not implied complete work:
- FluentValidation is not implemented yet
- EF Core migrations are not implemented yet; the current initializer still uses `EnsureCreated`
- audit logging is not implemented yet
- role management endpoints are not implemented yet
- employee write/delete flows are not implemented yet
- address CRUD and self-service address flows are not implemented yet
- Serilog, data-protection persistence, and the full cross-cutting hardening set from the approved plan are not implemented yet

## Working rule

When a planned architectural or implementation element is missing, record it here before moving forward so checkpoints stay honest and reviewable.
