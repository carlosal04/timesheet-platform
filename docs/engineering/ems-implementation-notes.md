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
- employee address detail endpoint `GET /employees/{employeeId}/addresses/{addressId}`
- employee address create endpoint `POST /employees/{employeeId}/addresses`
- employee address update endpoint `PUT /employees/{employeeId}/addresses/{addressId}`
- employee address primary-change endpoint `PATCH /employees/{employeeId}/addresses/{addressId}/primary`
- employee address delete endpoint `DELETE /employees/{employeeId}/addresses/{addressId}`
- self-service address list endpoint `GET /me/addresses`
- self-service address primary-change endpoint `PATCH /me/addresses/{addressId}/primary`
- self-service address delete endpoint `DELETE /me/addresses/{addressId}`
- role list endpoint `GET /roles`
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
- audit logging is partially implemented and currently covers auth, employee write/delete, and address create/read/update/primary-change/delete/list reads across admin and self-service paths
- role assignment endpoint is not implemented yet
- admin address endpoints are complete for the current admin address surface
- self-service address endpoints are complete for the current `/me/addresses` surface
- audit log read endpoint is not implemented yet
- Serilog, durable production-grade data-protection persistence, and the full cross-cutting hardening set from the approved plan are not implemented yet
- soft-delete rows currently capture `DeletedAtUtc`, but the schema does not yet persist `DeletedByUserId` even though the architecture notes mention it
- self-service address reads intentionally reuse the existing `AddressRead` policy; no separate `OwnAddressRead` policy has been introduced
- dedicated primary-change operations now use the approved `AddressPrimaryChanged` audit taxonomy instead of `AddressUpdated`
- canonical role names are aligned to the approved contract values `Administrator` and `Basic User`

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
- `GET /employees/{employeeId}/addresses/{addressId}`
- `POST /employees/{employeeId}/addresses`
- `PUT /employees/{employeeId}/addresses/{addressId}`
- `PATCH /employees/{employeeId}/addresses/{addressId}/primary`
- `DELETE /employees/{employeeId}/addresses/{addressId}`
- `GET /me/addresses`
- `PATCH /me/addresses/{addressId}/primary`
- `DELETE /me/addresses/{addressId}`
- `GET /roles`

Pending:
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
