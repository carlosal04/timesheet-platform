---
title: Employment Management System (EMS) — Codex Ingestion Rules
version: 1.4-draft
status: Review Pending
---

# 1. Purpose

This document exists to reduce ambiguity during AI-assisted implementation.

If generated code conflicts with this document, this document wins.

---

# 2. Canonical names

Use these names exactly:
- product module: `Employment Management System`
- short name: `EMS`
- future host product: `Time Sheet`

Do not use:
- `Employer Management System`
- `Employer`
- mixed abbreviations for the same module

---

# 3. Canonical technology choices

Use these choices exactly unless a later approved version changes them:
- backend target framework: `.NET 10`
- frontend framework: `Vue.js 3.5.30`
- frontend styling: `Tailwind CSS`
- logging provider: `Serilog`
- outbound HTTP resilience package: `Microsoft.Extensions.Http.Resilience`
- authentication model: server-validated cookie auth with persisted `UserSession`
- architecture style: Clean Architecture + CQRS + Wolverine + EF Core + PostgreSQL
- runtime model: Docker Compose with reverse proxy, frontend, backend, and PostgreSQL containers

---

# 4. Suggested solution and namespace strategy

Preferred structure inside this repository:
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

Shorter standalone alternative:
- `src/Ems.Api`
- `src/Ems.Application`
- `src/Ems.Domain`
- `src/Ems.Infrastructure`
- `src/Ems.Web`

Use one strategy consistently.

---

# 5. Generation constraints

Codex must not:
- skip the prerequisite and environment check before scaffolding
- implement features before the backend skeleton restores and builds cleanly
- create or modify repository text files in a non-UTF-8 encoding
- generate hard delete for employees
- generate hard delete for employee addresses
- expose soft-deleted employees by default
- expose soft-deleted addresses by default
- assume one address per employee
- skip audit logging for required events
- store passwords in plaintext
- implement ad hoc authorization with scattered role string comparisons
- use a freeform string column as the source of truth for roles
- put business rules in controllers or Vue components
- treat the client as the source of truth for session validity
- timezone-shift `DateOfBirth` or `HireDate`
- log passwords, cookies, anti-forgery tokens, or other secrets
- keep trying the same failing approach after two unsuccessful attempts without asking the user for help

---

# 6. Canonical domain decisions

Codex must implement these domain rules exactly:
- employee can have zero to many addresses
- employee can have zero or one active primary address
- active address collections return primary first
- employee delete is soft delete only
- address delete is soft delete only
- a soft-deleted employee is immutable in Phase 1
- a soft-deleted address is immutable in Phase 1
- `DateOfBirth` is required
- employee must be 21 years old or older on the current date
- `HireDate` is required and is a date-only value
- `HireDate` cannot be earlier than `DateOfBirth + 14 years`
- roles must come from the `Role` table with canonical codes `Admin` and `Basic`
- `User.EmployeeId` is the ownership link for Basic self-service address actions

---

# 7. API generation rules

Codex should:
- use RFC 7807 `ProblemDetails` for errors
- return UTC date-time values in ISO 8601 UTC format with trailing `Z`
- use ISO date-only strings for `DateOnly` fields
- return employee detail with `addresses[]`
- return address collections with primary first
- keep employee update separate from address CRUD
- use dedicated endpoints for changing the primary address
- include self-service endpoints under `/me/addresses` for Basic users

---

# 8. Security generation rules

Codex must:
- configure a global authenticated fallback policy
- use named authorization policies
- persist and validate `UserSession`
- enforce one active session per user
- apply anti-forgery to state-changing endpoints
- configure explicit CORS origins
- allow credentials only for approved origins
- persist data-protection keys
- keep security settings in configuration, not hard-coded literals
- revoke active sessions when a user role changes

---

# 9. Resilience and logging generation rules

Codex must:
- register outbound HTTP clients through `IHttpClientFactory`
- use `Microsoft.Extensions.Http.Resilience`
- apply 3 retry attempts after the initial request
- use retry delays of `2s`, `4s`, and `8s`
- configure Serilog as the application logging provider
- keep application code logging through `ILogger<T>` abstraction
- enrich logs with correlation ID when available

## 9.1 Container generation rules

Codex must:
- keep runtime files under `EMS/`
- define Dockerfiles and Compose files only after the backend skeleton is stable
- use a reverse proxy as the browser entrypoint
- serve built frontend assets from a dedicated frontend container
- define health checks for reverse proxy, frontend, backend, and PostgreSQL
- reject container image choices with known unfixed High or Critical CVEs unless explicitly approved

---

# 10. Audit generation rules

Codex must:
- use the dedicated audit taxonomy matrix as the canonical source for `ActionType`, `EntityType`, and `Result`
- keep audit writes append-only
- record required employee, address, and security events
- avoid inventing alternate action names once canonical names are defined
- record UTC timestamps in ISO 8601 UTC format with trailing `Z`

---

# 11. Frontend generation rules

Codex must:
- generate Vue code compatible with version `3.5.30`
- use Tailwind CSS for styling
- parse UTC date-time values to local display time in the frontend
- preserve date-only fields as date-only values
- send credentials on authenticated API requests when cookie auth is used
- support anti-forgery token/header handling for state-changing calls


# Additional v1.4 role-management constraints

- expose Admin-only `GET /roles` and `PATCH /users/{userId}/role` endpoints
- reject assignment of inactive roles
- revoke active sessions after successful role change
- prevent role changes that would leave the system with zero active Admin users
- audit both successful role assignments and rejected assignment attempts
