# EMS Implementation Notes

## Current checkpoint status

The current EMS foundation checkpoint includes:
- backend solution skeleton under `EMS/`
- Docker-based local runtime baseline
- cookie authentication with persisted user sessions
- repo-local data-protection key storage for the current local/test runtime
- employee read/list endpoints
- employee create/update/delete endpoints
- employee address list endpoint `GET /employees/{employeeId}/addresses`
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
- audit logging is partially implemented and currently covers auth, employee write/delete, and address-list reads
- role management endpoints are not implemented yet
- admin address endpoints other than `GET /employees/{employeeId}/addresses` are not implemented yet
- self-service address flows under `/me/addresses` are not implemented yet
- audit log read endpoint is not implemented yet
- Serilog, durable production-grade data-protection persistence, and the full cross-cutting hardening set from the approved plan are not implemented yet

## Endpoint implementation status

Implemented and verified:
- `POST /auth/login`
- `POST /auth/logout`
- `GET /auth/antiforgery`
- `GET /employees`
- `GET /employees/{id}`
- `POST /employees`
- `PUT /employees/{id}`
- `DELETE /employees/{id}`
- `GET /employees/{employeeId}/addresses`

Pending:
- `GET /employees/{employeeId}/addresses/{addressId}`
- `POST /employees/{employeeId}/addresses`
- `PUT /employees/{employeeId}/addresses/{addressId}`
- `PATCH /employees/{employeeId}/addresses/{addressId}/primary`
- `DELETE /employees/{employeeId}/addresses/{addressId}`
- `GET /me/addresses`
- `PATCH /me/addresses/{addressId}/primary`
- `DELETE /me/addresses/{addressId}`
- `GET /roles`
- `PATCH /users/{userId}/role`
- `GET /audit-logs`

## Frontend coordination rule

The active API contract under `docs/specs/EMS/current/Phase-01-api-contracts-ems-v1.4.md` must be updated whenever:
- an endpoint shape changes
- a request or response payload changes
- a query parameter is added, removed, or constrained
- a status-code behavior is intentionally changed

Implementation notes must continue to track what is already implemented versus what is still pending so frontend work can distinguish between approved contract and current backend availability.

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
