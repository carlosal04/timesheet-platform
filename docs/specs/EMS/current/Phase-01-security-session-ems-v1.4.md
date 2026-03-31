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

## 2.6 Onboarding and reset model
Phase 1 uses two distinct credential-recovery paths:
- onboarding uses Admin-issued temporary passwords delivered by email
- activated accounts use self-service reset tokens delivered by email

Rules:
- onboarding temporary passwords expire after 24 hours
- self-service reset tokens expire after 1 hour
- onboarding resend is Admin-only and only valid for onboarding or still-unactivated accounts
- temporary-password users must change their password before using normal business routes
- password-reset requests must not reveal whether an email exists

## 2.7 Renewal model
Phase 1 uses frontend-driven session renewal for active users only.

Rules:
- renewal is allowed only while the current session is still valid
- renewal extends idle timeout from the renewal time
- renewal does not silently recover an already expired session
- expired sessions require login again
- Phase 1 does not use a separate refresh-token model

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

For the current Phase 1 implementation:
- `POST /auth/login` is the enforced throttling point
- account lockout remains a separate control and does not replace endpoint throttling

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
- `HR`
- `Manager`
- `Developer`

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
- `MustChangePassword`
- `TemporaryPasswordExpiresAtUtc`
- `LastTemporaryPasswordIssuedAtUtc`
- `PasswordResetTokenHash`
- `PasswordResetTokenExpiresAtUtc`

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

The login response does not need to expose session timing if a dedicated authenticated session-bootstrap endpoint exists.
The login response must expose whether `mustChangePassword` is currently required.

## 5.2 Failed login
On failed login the system must:
1. write a `LoginFailed` audit event
2. increment failed access count where appropriate
3. lock the user temporarily when threshold is reached

The API must not reveal whether the email or password was wrong.

## 5.3 Temporary-password login
If the submitted password matches a still-valid onboarding temporary password, login may succeed only when:
1. the user is active
2. the temporary password has not expired
3. the account is still in the onboarding state

The resulting authenticated session must carry `mustChangePassword = true`.
Business endpoints other than logout, session bootstrap, anti-forgery bootstrap, session renew, and password change must reject access until the password is changed.

## 5.4 Forgot-password request
On `POST /auth/forgot-password` the system must:
1. return a generic success posture regardless of whether the email exists
2. issue a new random reset token only for active, activated users
3. hash the token before persistence
4. set a 1-hour expiry
5. send the reset email through the configured no-reply sender

## 5.5 Reset-password completion
On `POST /auth/reset-password` the system must:
1. validate the token hash and expiry
2. reject used, invalid, or expired tokens
3. set the new password hash
4. clear reset-token state
5. revoke active sessions for that user
6. write a `PasswordChanged` audit event

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

For Phase 1 renewal:
- authenticated reads may update `LastSeenAtUtc`
- renewal of `ExpiresAtUtc` happens only through the explicit renewal endpoint, not implicitly on every request

---

# 7. Logout flow

On logout the system must:
1. identify the current session
2. set `RevokedAtUtc`
3. set a revoke reason such as `UserLogout`
4. clear the auth cookie
5. write a `LogoutSucceeded` audit event

## 7.1 Session bootstrap flow
The API must expose an authenticated session-bootstrap endpoint so the frontend can discover current user identity and session timing after login and on page reload.

Minimum payload:
- user id
- email
- role code
- employee id when linked
- `mustChangePassword`
- session id
- current `ExpiresAtUtc`
- configured idle timeout minutes

## 7.2 Renewal flow
The API must expose an authenticated renewal endpoint.

On successful renewal the system must:
1. validate the current session and cookie principal
2. require anti-forgery validation
3. confirm the session is still active and not expired
4. extend `ExpiresAtUtc` from the current time using the configured idle timeout
5. update `LastSeenAtUtc`
6. reissue the authentication cookie with the new expiry
7. write a `SessionRenewed` audit event

If the session is already expired, revoked, or invalid, the endpoint must reject the request and the user must log in again.

---

# 8. Session invalidation events

The current session must be revoked when:
- the same user logs in again
- the user logs out
- the user account is disabled
- the user password changes
- the user receives a new onboarding temporary password
- the user completes a self-service password reset
- the user role changes
- an Admin reassigns the user to another role
- the user is locked out
- the session expires

Recommended revoke reasons:
- `ReLogin`
- `UserLogout`
- `PasswordChanged`
- `TemporaryPasswordReissued`
- `RoleChanged`
- `UserDisabled`
- `Lockout`
- `Expired`

Renewal is not a revocation event and must not create a replacement session when the existing session remains valid.

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

This includes:
- logout
- session renewal
- employee writes
- address writes
- role assignment

`POST /auth/login` is exempt in the current Phase 1 design because anti-forgery bootstrap is authenticated.

## 9.4 Frontend time handling
- UTC date-time values are parsed in the frontend and displayed as local time
- date-only values such as `DateOfBirth` and `HireDate` must remain date-only without timezone shifting

## 9.5 Frontend session-timer guidance
Frontend behavior should be:
- bootstrap current session timing from the authenticated session endpoint
- warn the user shortly before idle expiry
- call the renewal endpoint only while the session is still valid
- treat `401` on protected requests as session end and redirect to login
- if anti-forgery is invalid while auth is still valid, fetch a fresh anti-forgery token and retry once

---

# 11. Audit requirements for security events

The following audit events are required:
- `LoginSucceeded`
- `LoginFailed`
- `LogoutSucceeded`
- `SessionRenewed`
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

Phase 1 must use finite idle lifetimes.

Recommended baseline:
- moderate finite session lifetime for internal users
- no indefinite session
- avoid relying solely on sliding expiration
- server-side validation remains mandatory even with renewal
- no separate refresh token for Phase 1
- no silent session recovery after expiry

Exact duration is environment-configurable and must not be hard-coded in business logic.

---

# 12. Self-service authorization note

Manager and Developer users may perform self-service address actions only if:
- `User.EmployeeId` is not null
- the targeted address belongs to that employee
- the resource-based authorization check succeeds

Absence of a linked employee record must result in denial for self-service address mutation endpoints.

---

# 13. Production notes

1. Cookie auth, anti-forgery, and data protection must be treated as one security system.
2. A deployment that loses its key ring will invalidate existing cookies and anti-forgery payloads.
3. Any cross-origin browser deployment must be reviewed together with CORS, cookie policy, anti-forgery, and HTTPS.
