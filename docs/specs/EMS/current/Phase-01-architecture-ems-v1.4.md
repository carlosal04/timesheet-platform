---
title: Employment Management System (EMS) — Phase 1 Architecture
version: 1.4-draft
status: Review Pending
---

# 1. Architecture goals

The architecture must satisfy these goals from the beginning:
- support long-term growth into the Time Sheet platform
- keep business logic testable and isolated
- enforce production-grade security defaults
- support one active session per user
- make critical business changes auditable
- support a Vue browser client cleanly
- avoid ambiguous coding decisions during AI-assisted generation

---

# 2. Canonical stack

## 2.1 Backend
Phase 1 uses:
- **.NET 10**
- **ASP.NET Core**
- **Clean Architecture**
- **CQRS**
- **Wolverine** for command/query dispatch and pipeline behaviors
- **EF Core** with **Npgsql**
- **PostgreSQL**
- **Serilog** for application logging
- **Microsoft.Extensions.Http.Resilience** for outbound HTTP resilience

## 2.2 Frontend
Phase 1 browser client uses:
- **Vue.js v3.5.30**
- **Tailwind CSS**

## 2.3 Runtime and deployment baseline
Phase 1 uses a containerized runtime for both local development and deployment-aligned verification:
- **Docker Desktop / Docker Engine**
- **Docker Compose**
- one reverse-proxy container as the browser entrypoint
- one backend API container
- one frontend web container serving built Vue assets
- one PostgreSQL container

Approved runtime decision for the current greenfield phase:
- the PostgreSQL container baseline is approved to move from PostgreSQL 17 to PostgreSQL 18 in order to clear the High/Critical image gate
- the PostgreSQL production image reference must be pinned by digest rather than a floating tag
- local PostgreSQL 17 development volumes are disposable and must not be reused in place with a PostgreSQL 18 container
- any local move from PostgreSQL 17 to PostgreSQL 18 must use a fresh volume, rebuild, and migration-based schema initialization

Container images must:
- expose explicit health checks
- be reviewed for known vulnerabilities before use in the phase
- be blocked from promotion when unfixed High or Critical CVEs are present unless explicitly approved

---

# 3. Solution layout

Preferred modular layout inside this repository:
- `EMS/TimeSheet.Modules.EmploymentManagement.slnx`
- `EMS/NuGet.Config`
- `EMS/Directory.Build.props`
- `EMS/Directory.Packages.props`
- `EMS/src/TimeSheet.Modules.EmploymentManagement.Api`
- `EMS/src/TimeSheet.Modules.EmploymentManagement.Application`
- `EMS/src/TimeSheet.Modules.EmploymentManagement.Domain`
- `EMS/src/TimeSheet.Modules.EmploymentManagement.Infrastructure`
- `EMS/tests/TimeSheet.Modules.EmploymentManagement.*`
- `EMS/deploy/`

All backend projects must target `.NET 10`.

## 3.1 Skeleton-first delivery rule
Phase 1 implementation must start with infrastructure and project skeleton work before feature code.

Required order:
1. update approved EMS markdown docs
2. verify local prerequisites and repository hygiene
3. create the backend solution skeleton
4. prove restore, build, and test discovery
5. add container skeleton and health checks
6. only then start feature slices

If an implementation issue cannot be resolved in two attempts, stop and request user intervention before continuing.

---

# 4. Layer responsibilities

## 4.1 API Layer
Responsible for:
- route definitions
- request and response contracts
- authentication
- authorization policy enforcement
- anti-forgery validation
- CORS policy application
- rate limiting
- mapping HTTP requests to commands and queries

Must remain thin.

## 4.2 Application Layer
Responsible for:
- commands and queries
- orchestration
- validation
- transactional coordination
- audit event emission requests
- business workflows
- resource-aware authorization checks when required by resource state or ownership

Must not contain direct HTTP concerns.

## 4.3 Domain Layer
Responsible for:
- entities
- enums
- invariants
- canonical value sets and role codes

Must not depend on Infrastructure.

## 4.4 Infrastructure Layer
Responsible for:
- EF Core DbContext
- database mappings
- PostgreSQL access
- session persistence
- role seeding
- audit persistence
- hashing
- time provider
- outbound HTTP client registration
- Serilog sink configuration
- data-protection key persistence wiring

---

# 5. Canonical authentication model

## 5.1 Decision
Phase 1 uses **server-validated cookie authentication** backed by a persisted `UserSession` record.

## 5.2 Why this is the canonical choice
This module is a browser-based internal application with a hard requirement for:
- immediate logout
- immediate single-session invalidation
- revocation on password or role changes
- server-side control over session validity

A server-validated authentication cookie is the cleanest implementation for this use case.

## 5.3 Future compatibility
If a later phase needs external API or mobile clients, bearer tokens may be added in a later phase.  
That is out of scope for Phase 1.

---

# 6. Authorization model

## 6.1 Core rules
- deny by default
- require authentication globally using a fallback policy
- use named ASP.NET Core authorization policies
- avoid inline role checks scattered across endpoints
- enforce static authorization primarily at the API boundary
- use resource-based authorization when the rule depends on resource ownership or state

## 6.2 Phase 1 named policies
- `AuthenticatedUser`
- `AdminOnly`
- `EmployeeRead`
- `EmployeeWrite`
- `EmployeeDelete`
- `AddressRead`
- `AddressWrite`
- `AddressDeleteAny`
- `AddressPrimaryManageAny`
- `OwnAddressDelete`
- `OwnAddressPrimaryManage`
- `AuditLogRead`

## 6.3 Policy mapping
| Policy | Allowed roles | Notes |
|---|---|---|
| `AuthenticatedUser` | Admin, Basic | global authenticated usage |
| `EmployeeRead` | Admin, Basic | static read access |
| `EmployeeWrite` | Admin | create/update employees |
| `EmployeeDelete` | Admin | soft delete employees |
| `AddressRead` | Admin, Basic | visible address reads |
| `AddressWrite` | Admin | create/update addresses |
| `AddressDeleteAny` | Admin | soft delete any address |
| `AddressPrimaryManageAny` | Admin | set primary for any employee |
| `OwnAddressDelete` | Basic | requires resource-based ownership check |
| `OwnAddressPrimaryManage` | Basic | requires resource-based ownership check |
| `AuditLogRead` | Admin | audit list/read |

## 6.4 Ownership model for self-service
Basic self-service address actions are allowed only when:
- `User.EmployeeId` is populated
- the targeted address belongs to that employee
- the address is active and visible

## 6.5 Consistency rule
Static authorization is enforced through named policies at the API layer.  
Handlers must not duplicate static role checks.

When a rule depends on loaded data such as employee ownership, use resource-based authorization through the authorization service.

---

# 7. Request pipeline

Canonical request flow:
1. forwarded headers and HTTPS normalization if applicable
2. correlation ID enrichment
3. routing
4. CORS
5. authentication
6. authorization
7. anti-forgery validation for state-changing endpoints
8. rate limiting
9. endpoint mapping to command/query
10. validation
11. handler execution
12. persistence transaction
13. audit persistence
14. response generation

For containerized deployments, the reverse proxy sits in front of this pipeline and forwards browser traffic to the appropriate upstream service.

---

# 8. CQRS and Wolverine rules

## 8.1 Commands
Commands change state.

Examples:
- `LoginCommand`
- `LogoutCommand`
- `CreateEmployeeCommand`
- `UpdateEmployeeCommand`
- `SoftDeleteEmployeeCommand`
- `CreateEmployeeAddressCommand`
- `UpdateEmployeeAddressCommand`
- `SoftDeleteEmployeeAddressCommand`
- `SetPrimaryEmployeeAddressCommand`
- `SoftDeleteOwnEmployeeAddressCommand`
- `SetOwnPrimaryAddressCommand`

## 8.2 Queries
Queries do not change state.

Examples:
- `GetEmployeeByIdQuery`
- `ListEmployeesQuery`
- `ListEmployeeAddressesQuery`
- `GetEmployeeAddressByIdQuery`
- `GetMyAddressesQuery`
- `ListAuditLogsQuery`

## 8.3 Handler rules
- one handler per command/query
- handlers orchestrate use cases
- handlers do not contain HTTP logic
- resource ownership checks happen after loading the target resource and before mutation

---

# 9. Role and user architecture

## 9.1 Role model
Phase 1 must use a dedicated `Role` table.  
Users reference roles through `User.RoleId`.

Canonical seeded role codes:
- `Admin`
- `Basic`

Role codes are canonical application values and must be unique.
Only active roles are assignable to users.
Phase 1 exposes Admin-only role listing and user-role assignment APIs, but not role CRUD APIs.

## 9.2 Role-assignment architecture
Role assignment is an Admin-only security-sensitive command.

Minimum behavior:
- load the target user and target role
- reject the request if the role does not exist
- reject the request if the role is inactive
- reject the request if the target user is inactive when that rule is adopted by implementation
- prevent a change that would leave the system with zero active Admin users
- persist the new `User.RoleId` in one transaction
- revoke all active sessions for the affected user
- emit `UserRoleAssigned` and `SessionRevoked` audit events when applicable

Suggested commands/queries:
- `ListRolesQuery`
- `AssignUserRoleCommand`

## 9.3 User-to-employee link
`User.EmployeeId` is nullable and supports self-service ownership rules.

Expected usage:
- Admin users may have `EmployeeId = null`
- Basic users that need self-service address actions must have a non-null `EmployeeId`

A user must not point to more than one employee, and an employee must not be linked to more than one user.

---

# 10. Employee and address architecture

## 10.1 Employee aggregate direction
For Phase 1, employee and address data belong to the same bounded context and transaction boundary.

## 10.2 Address model
An employee can have multiple addresses.

Required rules:
- zero to many active addresses
- zero or one active primary address
- address type is required
- addresses are soft-deletable
- address collections must return primary first

## 10.3 Deterministic primary-address behavior
When a new primary address is chosen:
- the change happens in one transaction
- all other active addresses for that employee become non-primary

When the active primary address is soft-deleted:
- if another active address exists, the system promotes the oldest active non-deleted address by `CreatedAtUtc`
- if none exists, the employee has no active primary address

---

# 11. Soft delete architecture

## 11.1 Canonical implementation
Deletion in Phase 1 is soft delete only for both employees and addresses.

## 11.2 Employee delete behavior
`DELETE /employees/{id}` must:
- set `IsDeleted = true`
- set `DeletedAtUtc`
- set `DeletedByUserId`
- preserve the employee row
- preserve all addresses
- preserve all audit history

## 11.3 Address delete behavior
Admin endpoint `DELETE /employees/{employeeId}/addresses/{addressId}` and self-service endpoint `DELETE /me/addresses/{addressId}` must:
- set `IsDeleted = true`
- set `DeletedAtUtc`
- set `DeletedByUserId`
- preserve the address row
- preserve all audit history
- enforce primary-address replacement rules if needed

## 11.4 Query behavior
Employee and address queries must exclude soft-deleted rows by default.

`includeDeleted=true` is Admin-only.

## 11.5 Forbidden behavior
- no physical delete endpoint
- no background normalizer that physically deletes business rows
- no updates to a soft-deleted employee
- no updates to a soft-deleted address

---

# 12. Session architecture

## 12.1 Core design
Single-session support requires a persistent server-side session model.

Each login:
1. validates credentials
2. revokes any active session for the user
3. creates a new `UserSession`
4. issues a secure auth cookie tied to that session

Each authenticated request:
1. validates the cookie
2. resolves the current session
3. resolves the user and role
4. rejects the request if the session is missing, expired, revoked, or version-mismatched

Session renewal is explicit and frontend-driven, not automatic sliding expiration on every request.

Required endpoints:
- `GET /auth/session` for authenticated session bootstrap
- `POST /auth/renew` for extending an active session idle timeout

Renewal rules:
- renew only while the current session is still valid
- keep the same `UserSession.Id`
- extend `ExpiresAtUtc` from current server time
- reissue the cookie with the new expiry
- do not silently recover an expired session

## 12.2 Session invalidation triggers
The current session must become invalid when:
- the user logs out
- the same user logs in again
- the user password changes
- the user role changes or is reassigned
- the user account is disabled
- the user becomes locked out
- the session expires

---

# 13. Frontend/backend interaction architecture

## 13.1 Client style
The browser client is a Vue.js SPA or SPA-like application packaged as static assets and served from a dedicated frontend container.

## 13.2 Time handling
Frontend rules:
- parse UTC date-time values and display them in the user’s local time
- do not timezone-shift date-only values such as `DateOfBirth` and `HireDate`

## 13.3 Cookie and anti-forgery rules
For cookie-authenticated browser calls:
- the frontend must send credentials
- state-changing calls must include the anti-forgery token/header expected by the backend

Frontend session model:
- bootstrap current session state from `GET /auth/session`
- fetch anti-forgery material from `GET /auth/antiforgery`
- call `POST /auth/renew` explicitly when the user is active and expiry is near
- redirect to login when renewal or other protected calls return `401`

## 13.4 Preferred deployment pattern
Preferred production and local-compose deployment is reverse-proxied under a shared site boundary.

Canonical container traffic pattern:
- browser -> reverse proxy
- reverse proxy -> frontend web container for web routes
- reverse proxy -> backend API container for `/api` routes
- backend API container -> PostgreSQL container

This topology is preferred because it simplifies cookie handling, anti-forgery behavior, and browser security policy.

If separate origins are used:
- CORS must use an explicit origin allowlist
- credentials must be enabled explicitly
- wildcard origins are forbidden

## 13.5 Container health expectations
- the reverse-proxy container must report healthy only when its configuration is loaded and upstream routing is available
- the API container must expose health endpoints suitable for container health checks
- the frontend container must report healthy when the static site is being served
- the PostgreSQL container must use a database readiness check

## 13.6 PostgreSQL major-version change rule
Because EMS is still a greenfield module, the approved PostgreSQL 18 move is treated as a runtime-baseline decision rather than a data-migration project.

Required guardrails:
- do not silently float to PostgreSQL 18 through `latest`; use a pinned digest
- do not mount an existing PostgreSQL 17 data directory into a PostgreSQL 18 container
- validate the upgrade path with a fresh volume, health checks, EF migrations, and application smoke tests before considering the runtime baseline complete
