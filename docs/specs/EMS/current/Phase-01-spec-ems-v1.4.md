---
title: Employment Management System (EMS) — Phase 1 Specification
version: 1.4-draft
status: Review Pending
---

# 1. Canonical naming

## 1.1 Product and module name
The canonical bounded-context name is **Employment Management System (EMS)**.

Deprecated wording that must not be used anywhere:
- `Employer Management System`
- `Employer`
- any mixed naming that changes the EMS meaning

## 1.2 Relationship to the future Time Sheet project
EMS is the employee master-data and internal access-control foundation that will later be hosted inside **Time Sheet**.

Phase 1 keeps EMS as its own bounded context and source of truth for:
- employee master data
- employee address data
- internal user authentication
- internal role-based authorization
- auditability of security and business changes

---

# 2. Technology baseline

## 2.1 Backend baseline
Phase 1 must target **.NET 10** end to end.

Required baseline:
- target framework: `net10.0`
- ASP.NET Core on .NET 10
- EF Core aligned to .NET 10
- PostgreSQL provider aligned to .NET 10
- Wolverine aligned to .NET 10
- all NuGet dependencies must be .NET 10-compatible and pinned through a centralized versioning strategy

## 2.2 Frontend baseline
Phase 1 frontend uses:
- **Vue.js v3.5.30**
- **Tailwind CSS**

## 2.3 Logging and resilience baseline
Phase 1 must include:
- **Serilog** as the application logging provider
- `Microsoft.Extensions.Http.Resilience` for outbound HTTP resiliency
- explicit CORS configuration so frontend and backend can communicate safely

## 2.4 Runtime baseline
Phase 1 must support a Docker-based runtime baseline:
- backend API container
- frontend container
- PostgreSQL container
- reverse-proxy container
- health checks for all runtime services

---

# 3. Purpose

Phase 1 provides a production-grade foundation for managing employee records in a secure, auditable, maintainable, and implementation-ready way.

This phase is intentionally designed to avoid later rework when EMS evolves into the Time Sheet platform.

Phase 1 focuses on:
- authenticated browser access
- single active session per user
- policy-based and resource-aware authorization
- employee create, read, update, and soft delete
- employee address CRUD with soft delete
- append-only audit logging
- production-grade cross-cutting concerns from day one
- a container-ready delivery model that can be validated end to end

---

# 4. Phase 1 scope

## 4.1 Included
- login using email and password
- logout
- one active session per user
- Admin and Basic roles backed by a Roles table
- employee create
- employee update
- employee list with pagination
- employee detail
- employee soft delete
- address create
- address read/list
- address update
- address soft delete
- one primary address per employee
- Basic self-service address actions for the authenticated user’s own employee profile
- audit log write for critical security and business actions
- audit log read for Admin only
- minimal browser UI for login and employee browsing/editing
- CORS policy for Vue frontend integration
- structured application logging
- resilient outbound HTTP client registration baseline
- Docker Compose runtime for frontend, backend, reverse proxy, and PostgreSQL
- service health checks for runtime containers

## 4.2 Excluded
- hard delete
- employee restore/undelete endpoint
- address restore/undelete endpoint
- self-service user registration
- user management UI
- MFA
- external identity providers
- file uploads
- notifications
- reporting
- payroll
- time entry and time approval workflows

---

# 5. Roles

## 5.1 Admin
Can:
- log in and log out
- list employees
- view employee details
- create employees
- update employees
- soft delete employees
- create addresses for any employee
- read addresses for any visible employee
- update addresses for any employee
- soft delete addresses for any employee
- set or change the primary address for any employee
- view audit logs

## 5.2 Basic
Can:
- log in and log out
- list employees
- view employee details
- view addresses for visible employees
- soft delete addresses only for the employee profile linked to the authenticated user
- set or change the primary address only for the employee profile linked to the authenticated user

Cannot:
- create employees
- update employees
- soft delete employees
- create addresses for other employees
- update addresses for other employees
- view audit logs

---

# 6. Core use cases and acceptance criteria

## UC-01: Login
**Trigger:** User submits email and password.

**System behavior:**
1. Validate credentials.
2. Reject login if the user is inactive or locked out.
3. Revoke any currently active session for that user.
4. Create a new active session.
5. Issue a secure authentication cookie.
6. Write audit events.

**Acceptance criteria:**
- valid credentials create exactly one active session
- any prior active session becomes invalid immediately
- failed logins are audited
- successful login is audited

## UC-02: Logout
**Trigger:** Authenticated user requests logout.

**System behavior:**
1. Revoke the current session.
2. Clear the authentication cookie.
3. Write an audit event.

**Acceptance criteria:**
- the revoked session cannot access protected endpoints anymore
- logout is idempotent from the client perspective
- logout is audited

## UC-03: Create employee
**Trigger:** Admin submits new employee data.

**System behavior:**
1. Validate input.
2. Validate business rules, including age and email uniqueness.
3. Create the employee.
4. Optionally create zero or more addresses.
5. Enforce exactly zero or one primary address at create time.
6. Write audit events.

**Acceptance criteria:**
- Basic users cannot call this operation
- duplicate employee email is rejected
- employee is persisted only if validation passes
- create action is audited

## UC-04: Update employee
**Trigger:** Admin edits employee data.

**System behavior:**
1. Validate input.
2. Update allowed fields.
3. Preserve soft-delete and audit metadata.
4. Write audit events.

**Acceptance criteria:**
- Basic users cannot call this operation
- soft-deleted employees cannot be updated
- update action is audited

## UC-05: Soft delete employee
**Trigger:** Admin deletes an employee from the UI or API.

**System behavior:**
1. Mark the employee as deleted without removing the row.
2. Persist deletion metadata.
3. Exclude the employee from normal list/detail queries.
4. Preserve historical addresses and audit data.
5. Write audit events.

**Acceptance criteria:**
- no physical delete happens in Phase 1
- Basic users cannot call this operation
- repeated delete requests do not physically remove data
- soft delete is audited

## UC-06: List employees
**Trigger:** Authenticated user opens the employee list.

**System behavior:**
1. Return paginated employees.
2. Apply optional filters.
3. Exclude soft-deleted employees by default.
4. Allow `includeDeleted=true` only for Admin.
5. Return the primary address first when address data is included.

**Acceptance criteria:**
- Basic and Admin can use the endpoint
- deleted employees are hidden by default
- non-Admin cannot request deleted records

## UC-07: View employee details
**Trigger:** Authenticated user selects an employee.

**System behavior:**
1. Return employee data.
2. Return active addresses sorted with primary first.
3. Exclude soft-deleted employees unless Admin explicitly requests deleted data.

**Acceptance criteria:**
- Basic and Admin can view non-deleted employees
- deleted employees are not exposed to Basic users
- address list is deterministic and primary-first

## UC-08: Create address
**Trigger:** Admin creates an address for an employee.

**System behavior:**
1. Validate the address.
2. Persist the address.
3. If marked primary, demote any existing primary address for that employee.
4. Write audit events.

**Acceptance criteria:**
- Basic users cannot call this operation
- only one active primary address can exist per employee
- create action is audited

## UC-09: Update address
**Trigger:** Admin edits an address for an employee.

**System behavior:**
1. Validate the address.
2. Update the target address.
3. Re-evaluate primary-address rules if `isPrimary` changed.
4. Write audit events.

**Acceptance criteria:**
- Basic users cannot call this operation
- soft-deleted addresses cannot be updated
- update action is audited

## UC-10: Soft delete address
**Trigger:** Admin deletes any address, or Basic deletes an address belonging to the authenticated user’s own employee profile.

**System behavior:**
1. Mark the address as deleted without removing the row.
2. Preserve address history.
3. If the deleted address was primary and another active address exists, promote one replacement according to deterministic rules.
4. Write audit events.

**Acceptance criteria:**
- no physical delete happens in Phase 1
- Admin can soft delete any visible address
- Basic can soft delete only their own linked employee address
- delete action is audited

## UC-11: Change primary address
**Trigger:** Admin changes any employee primary address, or Basic changes the primary address for the authenticated user’s own employee profile.

**System behavior:**
1. Validate that the target address is active.
2. Demote any current active primary address for that employee.
3. Mark the target address as primary in the same transaction.
4. Write audit events.

**Acceptance criteria:**
- exactly zero or one active primary address exists after the operation
- Admin can change any employee primary address
- Basic can change primary only for their own linked employee profile
- primary change is audited


## UC-12: Assign role to user
**Trigger:** Admin assigns or changes a role for a user.

**System behavior:**
1. Validate the target user exists and is active.
2. Validate the target role exists and is active.
3. Reject the request if the target role is inactive.
4. Reject the request if the change would leave the system with zero active Admin users.
5. Persist the new `User.RoleId` in one transaction.
6. Revoke all active sessions for the affected user.
7. Write audit events.

**Acceptance criteria:**
- only Admin can assign or change a user role
- an inactive role cannot be assigned
- role changes are audited
- role changes revoke active sessions for the affected user
- the system never ends with zero active Admin users because of a role change

## UC-13: View roles
**Trigger:** Admin opens the role catalog for user-role management.

**System behavior:**
1. Return the canonical role list from the `Role` table.
2. Allow Admin to include inactive roles when needed.

**Acceptance criteria:**
- only Admin can list roles
- canonical role codes come from the `Role` table
- inactive roles are never assignable

## UC-14: View audit logs
**Trigger:** Admin opens audit logs.

**System behavior:**
1. Return paginated audit events.
2. Support filtering by actor, action, entity, result, and date range.

**Acceptance criteria:**
- only Admin can access audit logs
- audit events are append-only from the application perspective

---

# 7. Business rules and invariants

## 7.1 User and role rules
1. User email is required and unique.
2. Passwords must be stored only as secure password hashes.
3. A user can have only one active session at a time.
4. A locked-out user cannot log in.
5. An inactive user cannot log in.
6. User role is stored through a foreign key to the `Role` table, not a freeform string.
7. Only approved canonical role codes are valid in Phase 1: `Admin`, `Basic`.
8. A Basic user that needs self-service address actions must be linked to exactly one employee through `User.EmployeeId`.
9. Only Admin can assign or change the role of a user.
10. A role assignment target must exist in the `Role` table.
11. An inactive role cannot be assigned to a user.
12. A role change must revoke any active session for the affected user.
13. The application must not allow a role change that would leave the system with zero active users assigned to the `Admin` role.
14. Role assignments and role-change denials must be audited.
15. Phase 1 exposes role listing and role assignment APIs, but does not expose role create, update, activate, deactivate, or delete APIs.

## 7.2 Employee rules
1. First name is required.
2. Last name is required.
3. Employee email is required and unique among employees.
4. `DateOfBirth` is required.
5. `DateOfBirth` must be a valid past date.
6. The employee must be **21 years old or older on the current date**.
7. A birth date of yesterday is invalid because it fails the age rule.
8. `HireDate` is required.
9. `HireDate` is a date-only value.
10. `HireDate` cannot be earlier than `DateOfBirth + 14 years`.
11. `Status` must be a supported employee status value.
12. Soft-deleted employees are immutable in Phase 1.

## 7.3 Address rules
1. An employee may have zero to many addresses.
2. Address lines use normal business-address validation rules and max lengths defined in the API contract.
3. Address type is required.
4. Allowed address types in Phase 1:
   - `Home`
   - `Mailing`
   - `EmergencyContact`
   - `Other`
5. Each employee may have **at most one active primary address**.
6. An employee may have zero active primary addresses.
7. Soft-deleted addresses are immutable in Phase 1.
8. If the active primary address is soft-deleted and another active address exists, the system promotes the oldest active non-deleted address by `CreatedAtUtc`.
9. Basic self-service address operations are limited to the addresses that belong to the employee linked to `User.EmployeeId`.

## 7.4 Audit rules
1. Audit records are append-only.
2. Canonical `ActionType`, `EntityType`, and `Result` values come only from the audit taxonomy matrix.
3. Required UTC date-time fields in audits must use ISO 8601 UTC format with a trailing `Z`.

## 7.5 Time-format rules
1. All UTC date-time values exposed by the API must use ISO 8601 UTC format with a trailing `Z`.
2. `DateOfBirth` and `HireDate` are date-only values and must use ISO 8601 date format `yyyy-MM-dd`.
3. The frontend must convert UTC date-time values to local display time.
4. The frontend must not timezone-shift date-only values.

---

# 8. Non-functional requirements

## 8.1 Security
- secure cookie-based authentication
- anti-forgery on state-changing endpoints
- explicit CORS origin allowlist
- HTTPS outside development
- one active session per user
- rate limiting for authentication endpoints
- password hashing using framework-supported secure algorithms

## 8.2 Observability
- structured logging through Serilog
- correlation ID propagation where available
- audit trail for required security and business events

## 8.3 Performance and resilience
- paginated list endpoints
- outbound HTTP clients must use `Microsoft.Extensions.Http.Resilience`
- minimum retry schedule: 3 retries after the initial call with delays of 2s, 4s, and 8s

## 8.4 Maintainability
- clean separation between API, Application, Domain, and Infrastructure
- centralized role vocabulary through the `Role` table
- no ambiguous naming or undocumented alternate vocabularies
- skeleton-first project setup before feature implementation

## 8.5 Deployment and supply-chain baseline
- container images must define health checks
- reverse-proxy deployment is the preferred runtime topology
- images selected for the phase must be reviewed for known vulnerabilities
- unfixed High or Critical CVEs block image approval unless explicitly accepted

---

# 9. Delivery workflow guardrails

1. All EMS implementation code lives under `EMS/`.
2. Before feature work begins, confirm Git, `.gitignore`, tooling, and prerequisite availability.
3. Backend solution skeleton comes before business logic.
4. If an issue cannot be fixed in two attempts, stop and request user intervention.

---

# 10. Out of scope guardrails

The Phase 1 implementation must not:
- introduce hard delete for employee or address business data
- rely on freeform role strings for authorization decisions
- expose deleted rows by default
- store passwords, cookies, anti-forgery tokens, or secrets in logs
- timezone-shift `DateOfBirth` or `HireDate`
