# EMS Implementation Notes

## Current checkpoint status

The current EMS foundation checkpoint includes:
- backend solution skeleton under `EMS/`
- Docker-based local runtime baseline
- cookie authentication with persisted user sessions
- employee read/list endpoints
- Wolverine integrated as the API-to-application boundary for the implemented auth and employee read flows
- FluentValidation-based request validation for the currently implemented auth and employee read flows
- Application standardized to vertical slices with `Application/Abstractions/...` for interfaces and short in-slice CQRS naming
- repo-local EF tooling with a local `dotnet-ef` manifest and an initial baseline migration
- central package management, shared build props, and `*.slnx`
- local secret handling through `EMS/.env` with committed placeholders in `EMS/.env.example`

## Verified at this checkpoint

- solution restore succeeds
- solution build succeeds
- current tests pass

## Known gaps against the approved Phase 1 architecture/plan

The following items are still pending and should be treated as known implementation gaps, not implied complete work:
- audit logging is not implemented yet
- role management endpoints are not implemented yet
- employee write/delete flows are not implemented yet
- address CRUD and self-service address flows are not implemented yet
- Serilog, data-protection persistence, and the full cross-cutting hardening set from the approved plan are not implemented yet

## Review checklist for future EMS changes

Use this checklist when reviewing new EMS use cases:
- Application interfaces belong under `Application/Abstractions/`
- use cases live under `Application/<Area>/<UseCase>/`
- slice-local CQRS names are short: `Command`, `Query`, `Handler`, `Validator`, `Result`
- Wolverine remains the API-to-Application dispatch boundary
- validators stay in the same slice as the request they validate
- any deviation from the module standard is recorded here before merge

## Working rule

When a planned architectural or implementation element is missing, record it here before moving forward so checkpoints stay honest and reviewable.
