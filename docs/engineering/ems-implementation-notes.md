# EMS Implementation Notes

## Current checkpoint status

The current EMS foundation checkpoint implements the previously approved Phase 1 backend contract under `EMS/`:
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
- user list endpoint `GET /users`
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
- Wolverine integrated as the API-to-application boundary for the implemented EMS request flows
- FluentValidation-based request validation for the currently implemented EMS request flows
- Application standardized to vertical slices with `Application/Abstractions/...` for interfaces and short in-slice CQRS naming
- repo-local EF tooling with a local `dotnet-ef` manifest and an initial baseline migration
- central package management, shared build props, and `*.slnx`
- local secret handling through `EMS/.env` with committed placeholders in `EMS/.env.example`

## Approved recovery scope now pending

The approved current EMS spec set now goes beyond the code currently on this branch.

Newly approved but not yet implemented:
- canonical role-model expansion from `Admin/Basic` to `Admin/HR/Manager/Developer`
- separate Admin user provisioning via `POST /users`
- onboarding resend via `POST /users/{userId}/resend-temporary-password`
- forced first-login password change via `POST /auth/change-password`
- self-service reset flow via `POST /auth/forgot-password` and `POST /auth/reset-password`
- secure outbound email delivery from a no-reply sender
- lower-environment email capture for onboarding and reset verification

Until those slices land, the current codebase still runs on the older `Admin/Basic` implementation baseline even though the current spec set has been corrected.

## Verified runtime baseline

- the last shipped endpoint set matched the endpoint surface in `EMS/src/TimeSheet.Modules.EmploymentManagement.Api/Program.cs` before the newly approved access-recovery contract was added
- Docker CVE remediation is complete for the current approved baseline
- `03737bd` is the checkpoint that finalized the current Docker CVE remediation baseline
- `deploy-api` passes the High/Critical image gate
- `deploy-frontend` passes the High/Critical image gate
- `reverse-proxy` passes the High/Critical image gate
- the approved PostgreSQL 18 runtime image is pinned by digest in `EMS/deploy/compose.yaml`
- PostgreSQL 18 healthcheck verification succeeded locally on a fresh temporary Compose volume
- EF migration verification succeeded against PostgreSQL 18 on a fresh temporary Compose volume
- containerized application smoke verification against PostgreSQL 18 succeeded for health, login, session bootstrap, and employee list
- containerized reverse-proxy verification also succeeded for employee creation plus admin address create and update through the browser-facing `/api` path on an isolated throwaway stack
- local Windows verification required moving the default published PostgreSQL host port from `54329` to `15432` because the original range was excluded on this machine
- the standalone local-host PostgreSQL 18 smoke path in this shell proved unreliable; the trusted verification path for the PostgreSQL 18 upgrade is the containerized runtime path
- solution-level `dotnet build` and `dotnet test` now pass again in this shell under `.NET SDK 10.0.201`

## Audit coverage notes

- the minimum required Phase 1 audit set from `docs/specs/EMS/current/Phase-01-audit-taxonomy-matrix-ems-v1.4.md` is implemented for auth/session flows, employee writes/deletes, address reads/writes/deletes across admin and self-service paths, role assignment outcomes, authorization denials, and audit-log reads
- employee read/list audit taxonomy values exist, but employee read/list endpoints do not currently emit those audit events
- employee read/list auditing is not part of the minimum required Phase 1 audit set and should not be treated as implemented unless explicitly added later

## Known environment and tooling issues

- the previous `.NET SDK 10.0.200` `MSB4276` workload-resolver issue was resolved after repairing the local .NET 10 / Visual Studio installation and moving to `.NET SDK 10.0.201`
- local EF migration generation still succeeds through `dotnet-ef --no-build` after a successful build path, and the repo EF helper reuses the already-restored local tool before attempting restore
- a full rebuild immediately after running tests can still show a transient locked-PDB warning if `testhost` has not released a test assembly yet; current solution build/test verification remains green

## Current hardening follow-ups

- the EMS backend now implements the approved `GET /users` admin endpoint needed by the role-assignment UI
- no remaining Docker CVE remediation work is pending for the current approved baseline
- the frontend, reverse-proxy, PostgreSQL, and API Docker image references are now pinned to the verified artifacts used for the current runtime baseline
- data-protection keys now persist for the current local and Docker single-instance runtime, but a shared/protected key-ring strategy would still be needed before multi-instance production deployment
- approved phase-completion changes still need to be reflected in EMS markdown before archiving

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
- `GET /users`
- `PATCH /users/{userId}/role`
- `GET /audit-logs`

Pending:
- role-model migration from `Admin/Basic` to `Admin/HR/Manager/Developer`
- `POST /users`
- `POST /users/{userId}/resend-temporary-password`
- `POST /auth/change-password`
- `POST /auth/forgot-password`
- `POST /auth/reset-password`
- outbound email infrastructure and lower-environment email capture
- frontend recovery work for new roles, onboarding, reset-password, and copy/alignment cleanup

## Last shipped backend-ready summary for UI planning

This section describes the last shipped frontend/backend baseline before the approved access-recovery scope.
Do not treat it as a statement that the new onboarding/reset endpoints are already implemented.

Last shipped backend-ready summary:
- implemented endpoint surface:
  - auth: `POST /auth/login`, `POST /auth/logout`, `GET /auth/antiforgery`, `GET /auth/session`, `POST /auth/renew`
  - employees: `GET /employees`, `GET /employees/{id}`, `POST /employees`, `PUT /employees/{id}`, `DELETE /employees/{id}`
  - admin addresses: `GET /employees/{employeeId}/addresses`, `GET /employees/{employeeId}/addresses/{addressId}`, `POST /employees/{employeeId}/addresses`, `PUT /employees/{employeeId}/addresses/{addressId}`, `PATCH /employees/{employeeId}/addresses/{addressId}/primary`, `DELETE /employees/{employeeId}/addresses/{addressId}`
  - self-service addresses: `GET /me/addresses`, `PATCH /me/addresses/{addressId}/primary`, `DELETE /me/addresses/{addressId}`
  - admin reads: `GET /roles`, `GET /users`, `PATCH /users/{userId}/role`, `GET /audit-logs`
- auth and session model:
  - cookie authentication with persisted `UserSession`
  - idle-timeout session model with frontend-driven renew through `POST /auth/renew`
  - `GET /auth/session` is the canonical session-bootstrap endpoint for page load and post-login bootstrap
- anti-forgery behavior:
  - all authenticated state-changing requests require a valid anti-forgery token and cookie pair
  - `POST /auth/login` is exempt in the approved Phase 1 design
  - `GET /auth/antiforgery` returns the header contract using `X-CSRF-TOKEN`
- expected status posture:
  - `400` for validation failures and invalid anti-forgery
  - `401` for unauthenticated, expired, or revoked session access
  - `403` for explicit denied ownership or authorization outcomes where the contract requires denial instead of hiding
  - `404` for hidden or missing resources where the contract requires not found
  - `409` for business conflicts such as repeated soft-delete or invalid role-change conflicts
  - `429` for the login rate limit on `POST /auth/login`
- runtime assumptions the frontend should know:
  - the Docker runtime baseline is verified with pinned frontend, reverse-proxy, PostgreSQL 18, and API image references
  - the trusted verification path for PostgreSQL 18 is the containerized runtime path, not the noisy direct local-host shell probe
  - current local shell-level `dotnet build` and `dotnet test` are blocked by the documented `.NET 10.0.200` toolchain issue until the SDK installation is repaired

## Frontend coordination rule

The active API contract under `docs/specs/EMS/current/Phase-01-api-contracts-ems-v1.4.md` must be updated whenever:
- an endpoint shape changes
- a request or response payload changes
- a query parameter is added, removed, or constrained
- a status-code behavior is intentionally changed

Implementation notes must continue to track what is already implemented versus what is still pending so frontend work can distinguish between approved contract and current backend availability.

## Frontend design-first gate

Before creating `ui/`, the EMS frontend must pass a design-review gate using:
- `docs/engineering/ems-ui-screen-pack.md`
- `docs/engineering/ems-ui-screen-pack.html`

This gate exists so frontend implementation starts from approved screens instead of chat context.

Current gate status:
- high-fidelity screen pack prepared and approved
- `ui/` scaffold is now created from the approved screen pack
- `ui/.editorconfig` keeps LF line endings
- the approved frontend renew posture remains a 30-minute warning window before expiry, with explicit renew through `POST /auth/renew` while the session is still valid

## Frontend foundation status

The current EMS frontend foundation under `ui/` implements:
- Vite + Vue + TypeScript scaffold created with Router, Pinia, Vitest, ESLint, and Prettier
- Nuxt UI core integrated for the Vue/Vite app, with Tailwind and a semantic light/dark token layer
- client-side theme selection with:
  - system theme on first visit
  - user override remembered in local storage
  - top-bar toggle in the authenticated shell
- desktop-first authenticated shell with:
  - top bar
  - role-aware left navigation
  - session-warning banner
  - shared page-header pattern
- real auth/session integration with:
  - `POST /auth/login`
  - `POST /auth/logout`
  - `GET /auth/session`
  - `GET /auth/antiforgery`
  - `POST /auth/renew`
  - role-aware route guards backed by session bootstrap on reload
  - countdown-driven 30-minute warning window
  - explicit renew action against the real backend
  - redirect back to `/login` on logout or unauthorized session loss
  - centralized anti-forgery bootstrap and one-time refresh/retry support for authenticated state-changing requests
- mock-mode route coverage for:
  - employee create/edit
  - employee addresses
  - my addresses
  - roles
  - audit logs
  - `403`
  - `404`
- real employee read integration with:
  - `GET /employees` for the employee list page
  - `GET /employees/{id}` for the employee detail page
  - backend-driven pagination/filter state for the list page
  - unauthorized redirect back to `/login` when employee reads lose the current session
  - mock fallback kept only for untouched placeholder employee routes that still reference fixture ids
- real employee write integration with:
  - `POST /employees` for the create form
  - `PUT /employees/{id}` for the edit form
  - `DELETE /employees/{id}` from the employee detail screen
  - real `400/404/409` handling through shared problem and validation states
  - detail-page posture adjusted so Admin users stay on the admin address-management route while Basic users only see `/me/addresses` for their own linked employee record
- real address screen integration with:
  - `GET /employees/{employeeId}/addresses` for the admin address-management screen
  - `POST /employees/{employeeId}/addresses` for admin address creation
  - `PUT /employees/{employeeId}/addresses/{addressId}` for admin address editing
  - `GET /me/addresses` for the self-service address screen
  - `PATCH /employees/{employeeId}/addresses/{addressId}/primary` and `DELETE /employees/{employeeId}/addresses/{addressId}` for admin address actions
  - `PATCH /me/addresses/{addressId}/primary` and `DELETE /me/addresses/{addressId}` for self-service address actions
  - employee detail now links to the live admin address screen for real employee records
  - admin address create/edit uses the real backend contract, canonical address-type values, and shared validation/problem handling
- real audit-log integration with:
  - `GET /audit-logs` for the admin audit page
  - backend-driven filtering by actor user, action type, entity type, and result
  - backend-driven paging posture with live audit metadata instead of mock summaries
- real role-screen integration with:
  - `GET /roles` for the canonical role catalog
  - `GET /users` for the admin-selectable user list with paging and filtering
  - `PATCH /users/{userId}/role` for per-row role changes
  - local inline success and problem feedback for role-change outcomes, including session-revocation counts
- verified reverse-proxy runtime path with the real UI:
  - rebuilt the frontend and API containers from the current repo state
  - served the UI through `http://localhost:8088/`
  - verified `/`, `/healthz`, and `/api/health`
  - verified login, `GET /auth/session`, and `GET /auth/antiforgery` through the reverse proxy
  - verified `GET /roles` and `GET /users` through the reverse proxy
  - verified one successful user-role change through `PATCH /users/{userId}/role` on a throwaway seeded user in the temporary runtime database
  - verified one successful employee create plus admin address create and update flow through the reverse proxy on a throwaway seeded employee in the temporary runtime database
  - used an isolated throwaway Compose project with explicit test-only env vars because `EMS/.env` was not present in this workspace
- shared frontend states and primitives for:
  - loading skeleton
  - empty state
  - validation summary
  - problem-state panel
  - confirmation dialog
- local Vite proxy support for `/api` via `VITE_DEV_API_TARGET`

Verified frontend foundation baseline:
- `npm run build` passes in `ui/`
- `npm run lint` passes in `ui/`
- `npm run test:unit -- --run` passes in `ui/`
- the frontend Docker image now builds from `ui/` rather than the placeholder static site
- the built frontend container passes `/healthz` and the nginx healthcheck

Current frontend pending work:
- approved current specs now require access-recovery work beyond the current UI baseline
- the next frontend slices are role-model migration, user provisioning, onboarding resend, forced password change, self-service reset, and relevant copy/alignment cleanup

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
