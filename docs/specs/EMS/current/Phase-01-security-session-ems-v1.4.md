---
title: Employment Management System (EMS) — Phase 1 Security and Single-Session Design
version: 1.4-draft
status: Review Pending
---

# 1. Goal

This document defines the production-grade security baseline for Phase 1 and the exact requirements needed to support **one active session per user**.

---

# 2. Canonical decisions

## 2.1 Authentication transport
Phase 1 uses a secure authentication cookie.

## 2.2 Session authority
The server is the source of truth for whether a session is valid.

## 2.3 Single-session rule
Each user may have **at most one active session** at any time.

## 2.4 Immediate revocation
Logout, re-login, password change, role change, account disable, or lockout must invalidate access immediately on the server side.

## 2.5 Role integrity
User role assignment must be stored through a `Role` table and a foreign key from `User.RoleId`, not through a freeform string column.
Only active roles are assignable to users.
Role assignment is an Admin-only security operation.

---

# 3. Required security controls

## 3.1 Cookie requirements
The authentication cookie must:
- be HTTP-only
- be Secure outside development
- use a finite expiration
- be cleared on logout
- never contain raw secrets in plaintext
- use a `SameSite` mode appropriate to the deployment topology

## 3.2 Deployment-specific `SameSite` rules
Preferred production topology is same-site or reverse-proxied deployment.

Rules:
- if frontend and backend are same-site, prefer `SameSite=Lax` or stricter based on endpoint behavior
- if frontend and backend are cross-site and the browser must send the auth cookie, use `SameSite=None` and `Secure=true`
- deployment must not rely on default browser behavior

## 3.3 Anti-forgery
Because authentication is cookie-based, all state-changing endpoints require anti-forgery protection.

## 3.4 HTTPS
HTTPS is mandatory outside development.

## 3.5 Password handling
- passwords are hashed
- passwords are never encrypted for later retrieval
- passwords are never logged
- password verification must use a framework-supported hashing approach

## 3.6 Lockout
The system must track failed access attempts and temporarily lock accounts after repeated failures.

## 3.7 Rate limiting
Authentication endpoints must use stricter rate limiting than standard business endpoints.

## 3.8 Key management
ASP.NET Core data-protection keys must be persisted and protected so auth cookies remain valid across restarts and scaled instances.

## 3.9 Security logging
Security-relevant outcomes must be both logged and audited when applicable, without exposing secrets.

## 3.10 Time serialization rule
All security-related UTC date-time values returned by the API or persisted in audit logs must use ISO 8601 UTC format with a trailing `Z`.

---

# 4. Required data model additions

## 4.1 Role
The `Role` entity must include at least:
- `Code`
- `Name`
- `IsActive`

Canonical seeded role codes for Phase 1:
- `Admin`
- `Basic`

Recommended additional field:
- `IsSystem` to protect seeded roles from destructive mutation

## 4.2 User
The `User` entity must include:
- `RoleId`
- `EmployeeId`
- `IsActive`
- `AccessFailedCount`
- `LockoutEndUtc`
- `SessionVersion`

## 4.3 UserSession
A `UserSession` table is required with at least:
- session identifier
- user identifier
- session version snapshot
- created time
- last seen time
- expiration time
- revoked time
- revoke reason

---

# 5. Login flow

## 5.1 Successful login
On successful login the system must:
1. validate credentials
2. resolve the user role from the `Role` table
3. verify user is active
4. verify user is not locked out
5. revoke any currently active session for that user
6. increment or validate `User.SessionVersion` if the implementation uses it for forced invalidation semantics
7. create a new `UserSession`
8. issue an authentication cookie tied to that session
9. write a `LoginSucceeded` audit event

## 5.2 Failed login
On failed login the system must:
1. write a `LoginFailed` audit event
2. increment failed access count where appropriate
3. lock the user temporarily when threshold is reached

The API must not reveal whether the email or password was wrong.

---

# 6. Authenticated request validation

Every protected request must validate all of the following:
1. cookie is present
2. cookie can be decrypted and validated
3. principal contains user identifier and session identifier
4. user still exists
5. user role reference is valid
6. user is active
7. user is not locked out
8. session exists
9. session is not revoked
10. session is not expired
11. session version matches the user version if version-based invalidation is enabled

If any check fails, the request is rejected and the session should be treated as invalid.

---

# 7. Logout flow

On logout the system must:
1. identify the current session
2. set `RevokedAtUtc`
3. set a revoke reason such as `UserLogout`
4. clear the auth cookie
5. write a `LogoutSucceeded` audit event

---

# 8. Session invalidation events

The current session must be revoked when:
- the same user logs in again
- the user logs out
- the user account is disabled
- the user password changes
- the user role changes
- an Admin reassigns the user to another role
- the user is locked out
- the session expires

Recommended revoke reasons:
- `ReLogin`
- `UserLogout`
- `PasswordChanged`
- `RoleChanged`
- `UserDisabled`
- `Lockout`
- `Expired`

---

# 9. Role-assignment security rules

1. Only Admin can assign or change a user role.
2. The target role must exist and be active.
3. An inactive role must return a validation or conflict failure and must not be assigned.
4. A role change must revoke any active sessions for the affected user immediately.
5. The application must not allow a role change that would leave the system with zero active Admin users.
6. Role assignment and role-assignment denial outcomes must be audited.

---

# 10. CORS and browser integration rules

## 9.1 CORS
The API must define an explicit CORS allowlist for approved frontend origins.

## 9.2 Credentials
If the frontend and backend are on different origins and the auth cookie must flow between them:
- credentials must be enabled explicitly
- wildcard origins are forbidden
- the frontend must send credentials on authenticated requests

## 9.3 Anti-forgery transport
The frontend must fetch and send the anti-forgery token/header expected by the backend for all state-changing operations.

## 9.4 Frontend time handling
- UTC date-time values are parsed in the frontend and displayed as local time
- date-only values such as `DateOfBirth` and `HireDate` must remain date-only without timezone shifting

---

# 11. Audit requirements for security events

The following audit events are required:
- `LoginSucceeded`
- `LoginFailed`
- `LogoutSucceeded`
- `SessionRevoked`
- `AccessDenied` when explicitly captured
- `EmployeeCreated`
- `EmployeeUpdated`
- `EmployeeSoftDeleted`
- `AddressCreated`
- `AddressUpdated`
- `AddressPrimaryChanged`
- `AddressSoftDeleted`

Each audit event should capture:
- actor user ID when available
- session ID when available
- target entity type and ID when relevant
- result
- UTC timestamp in ISO 8601 UTC format with trailing `Z`
- correlation ID
- hashed client identifiers if captured

Canonical allowed values come from the audit taxonomy matrix.

---

# 11. Session expiration guidance

Phase 1 must use finite lifetimes.

Recommended baseline:
- moderate finite session lifetime for internal users
- no indefinite session
- avoid relying solely on sliding expiration
- server-side validation remains mandatory even with renewal

Exact duration is environment-configurable and must not be hard-coded in business logic.

---

# 12. Self-service authorization note

Basic users may perform self-service address actions only if:
- `User.EmployeeId` is not null
- the targeted address belongs to that employee
- the resource-based authorization check succeeds

Absence of a linked employee record must result in denial for self-service address mutation endpoints.

---

# 13. Production notes

1. Cookie auth, anti-forgery, and data protection must be treated as one security system.
2. A deployment that loses its key ring will invalidate existing cookies and anti-forgery payloads.
3. Any cross-origin browser deployment must be reviewed together with CORS, cookie policy, anti-forgery, and HTTPS.
