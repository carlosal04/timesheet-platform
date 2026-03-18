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
- user-role assignment endpoint `PATCH /users/{userId}/role`
- audit log read endpoint `GET /audit-logs`
- authenticated session bootstrap endpoint `GET /auth/session`
- authenticated session renew endpoint `POST /auth/renew`
- shared anti-forgery enforcement for authenticated state-changing endpoints, with `POST /auth/login` exempt
- config-driven login rate limiting on `POST /auth/login`
- soft-delete metadata now persists both `DeletedAtUtc` and `DeletedByUserId` for employees and addresses
- Docker frontend static container moved to Chainguard nginx with an image-compatible `nginx -t` healthcheck and a custom root nginx config for clean non-root startup
- Docker reverse proxy moved to Chainguard nginx with an image-compatible `nginx -t` healthcheck
- explicit CORS allowlist configuration for approved frontend origins
- config-driven data-protection key persistence for local and containerized single-instance runtime
- Serilog host-level logging baseline
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
- audit logging is partially implemented and currently covers auth/session flows, employee read/write/delete flows, address read/write/delete flows across admin and self-service paths, role assignment outcomes, and audit-log reads
- role management endpoints are implemented for role list and user-role assignment
- admin address endpoints are complete for the current admin address surface
- self-service address endpoints are complete for the current `/me/addresses` surface
- Docker image CVE remediation is not implemented yet
- Docker image CVE remediation is in progress:
  - frontend static container has been moved off `nginx:1.29-alpine` and verified healthy locally
  - reverse proxy has been moved off `nginx:1.29-alpine` and verified healthy locally
  - PostgreSQL has been moved off `postgres:17-alpine` to `postgres:17-bookworm` and still needs runtime verification in Compose plus a fresh post-change scan result
  - standalone PostgreSQL 17 Bookworm runtime and `pg_isready` healthcheck compatibility were verified locally
  - the previous Compose verification failure was traced to a Windows excluded TCP port range that covers the old local defaults `54329` and `54331`; the repo default local PostgreSQL host port has been moved to `15432`
- the frontend-driven session-renew model is partially implemented:
  - `GET /auth/session`
  - `POST /auth/renew`
  - `SessionRenewed` audit taxonomy
  - shared anti-forgery enforcement now covers renew and the other authenticated state-changing endpoints
- data-protection keys now persist for the current local and Docker single-instance runtime, but a shared/protected key-ring strategy would still be needed before multi-instance production deployment
- self-service address reads intentionally reuse the existing `AddressRead` policy; no separate `OwnAddressRead` policy has been introduced
- dedicated primary-change operations now use the approved `AddressPrimaryChanged` audit taxonomy instead of `AddressUpdated`
- canonical role names are aligned to the approved contract values `Administrator` and `Basic User`
- successful role changes revoke active sessions for the affected user and emit `SessionRevoked` plus `UserRoleAssigned` audit events
- local EF migration generation currently succeeds through `dotnet-ef --no-build` after a successful solution build; direct startup-project builds still fail opaquely in this shell
- the repo EF helper now supports `-NoBuild` and reuses the already-restored local tool before attempting restore

## Endpoint implementation status

Implemented and verified:
- `POST /auth/login`
- `POST /auth/logout`
- `GET /auth/antiforgery`
- `GET /auth/session`
- `POST /auth/renew`
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
- `PATCH /users/{userId}/role`
- `GET /audit-logs`

Pending:
- no remaining business endpoints in the current approved EMS Phase 1 backend contract
- cross-cutting hardening and runtime-completion items only
- next runtime/security hardening priority:
  - Docker image CVE remediation

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
