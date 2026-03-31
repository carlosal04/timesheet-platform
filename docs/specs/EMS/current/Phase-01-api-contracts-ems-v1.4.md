---
title: Employment Management System (EMS) — Phase 1 API Contracts
version: 1.4-draft
status: Review Pending
---

# 1. Contract rules

## 1.1 Format
- request and response bodies use JSON
- errors use RFC 7807 `ProblemDetails`
- authentication is cookie-based
- UTC date-time values use ISO 8601 UTC strings with trailing `Z`
- date-only values use ISO 8601 date format `yyyy-MM-dd`

Example UTC date-time:
```text
2026-03-16T17:21:43Z
```

## 1.2 Visibility
- soft-deleted employees are hidden by default
- soft-deleted addresses are hidden by default
- unauthorized deleted-record access should return `404` or `403` according to endpoint posture

## 1.3 Collection ordering
Whenever an employee address collection is returned, active addresses are sorted:
1. primary address first
2. other active addresses by `createdAtUtc ASC`

## 1.4 Anti-forgery
- all authenticated state-changing requests (`POST`, `PUT`, `PATCH`, `DELETE`) require a valid anti-forgery token and matching anti-forgery cookie
- `POST /auth/login` is exempt in the current Phase 1 design because anti-forgery bootstrap is authenticated
- missing or invalid anti-forgery validation returns `400 Bad Request` with `ProblemDetails`

---

# 2. Authentication endpoints

## 2.1 POST `/auth/login`
Authenticates a user and issues the session cookie.

### Request
```json
{
  "email": "admin@company.com",
  "password": "PlaintextPasswordOnlyInTransit"
}
```

### Success
- Status: `200 OK`
```json
{
  "userId": "guid",
  "email": "admin@company.com",
  "roleCode": "Admin",
  "mustChangePassword": false
}
```

### Errors
- `400` invalid payload
- `401` invalid credentials
- `423` locked out
- `429` too many attempts

### Required behavior
- login throttling is enforced on this endpoint
- other authentication endpoints do not inherit the login throttle automatically

## 2.2 POST `/auth/logout`
Revokes the current session and clears the cookie.

### Request
No body required.

### Success
- Status: `204 No Content`

### Errors
- `400` missing or invalid anti-forgery token

## 2.3 GET `/auth/antiforgery`
Returns the anti-forgery request token/header contract for the current authenticated session.

### Success
- Status: `200 OK`
```json
{
  "headerName": "X-CSRF-TOKEN",
  "requestToken": "opaque-token-value"
}
```

### Errors
- `401` session missing, expired, revoked, or otherwise invalid

## 2.4 GET `/auth/session`
Returns the current authenticated user/session snapshot for frontend bootstrap and timer synchronization.

### Success
- Status: `200 OK`
```json
{
  "userId": "guid",
  "email": "admin@company.com",
  "roleCode": "Admin",
  "employeeId": "guid or null",
  "mustChangePassword": false,
  "sessionId": "guid",
  "expiresAtUtc": "2026-03-18T15:30:00Z",
  "idleTimeoutMinutes": 480
}
```

### Errors
- `401` session missing, expired, revoked, or otherwise invalid

## 2.5 POST `/auth/renew`
Extends the current authenticated session when it is still valid. This endpoint is frontend-driven and is intended for active users nearing idle timeout.

### Request
No body required.

### Required headers
- anti-forgery header `X-CSRF-TOKEN`

### Success
- Status: `200 OK`
```json
{
  "sessionId": "guid",
  "expiresAtUtc": "2026-03-18T16:00:00Z",
  "idleTimeoutMinutes": 480
}
```

### Errors
- `400` missing or invalid anti-forgery token
- `401` session missing, expired, revoked, or otherwise invalid

### Required behavior
- renewal is allowed only while the current session is still valid
- renewal keeps the same session identifier
- renewal extends the session idle timeout from the current time
- renewal reissues the authentication cookie with the new expiry
- expired sessions are not silently recovered; the user must log in again

## 2.6 POST `/auth/change-password`
Changes the current authenticated user's password. This endpoint is used for both normal password change and the forced first-login password change after temporary-password onboarding.

### Request
```json
{
  "currentPassword": "CurrentOrTemporaryPassword",
  "newPassword": "NewStrongPassword123!"
}
```

### Success
- Status: `204 No Content`

### Errors
- `400` invalid payload, invalid current password, or password-policy failure
- `401` session missing, expired, revoked, or otherwise invalid

### Required behavior
- anti-forgery validation is required
- a successful change clears `mustChangePassword`
- password change revokes any older active sessions for the user

## 2.7 POST `/auth/forgot-password`
Starts a self-service password reset for an activated user account.

### Request
```json
{
  "email": "user@company.com"
}
```

### Success
- Status: `202 Accepted`

### Required behavior
- this endpoint is anonymous
- the response must not reveal whether the email exists
- only activated accounts may receive self-service reset emails
- reset delivery happens through a side-channel email, not in the response body

## 2.8 POST `/auth/reset-password`
Consumes a single-use password-reset token and sets a new password.

### Request
```json
{
  "token": "opaque-reset-token",
  "newPassword": "NewStrongPassword123!"
}
```

### Success
- Status: `204 No Content`

### Errors
- `400` invalid payload, invalid token, expired token, or password-policy failure

### Required behavior
- this endpoint is anonymous
- reset tokens are single-use and short-lived
- successful reset clears any pending reset state and revokes existing active sessions

---

# 3. Employee endpoints

## 3.1 GET `/employees`
Returns paginated employees.

### Query parameters
| Name | Type | Notes |
|---|---|---|
| `page` | int | default 1 |
| `pageSize` | int | bounded |
| `name` | string? | optional contains filter |
| `status` | string? | `Active` / `Inactive` |
| `hireDateFrom` | date? | optional |
| `hireDateTo` | date? | optional |
| `includeDeleted` | bool? | Admin only; default false |
| `includePrimaryAddress` | bool? | optional; default false |

### Success
- Status: `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "firstName": "Ana",
      "lastName": "Lopez",
      "email": "ana.lopez@company.com",
      "phone": "5551234567",
      "dateOfBirth": "1990-06-18",
      "hireDate": "2025-01-15",
      "status": "Active",
      "primaryAddress": {
        "id": "guid",
        "addressType": "Home",
        "isPrimary": true,
        "line1": "100 Main St",
        "line2": null,
        "city": "Pittsburgh",
        "state": "PA",
        "zipCode": "15222",
        "countryCode": "US"
      }
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 1
}
```

## 3.2 GET `/employees/{id}`
Returns a single employee with active addresses ordered primary first.

### Success
- Status: `200 OK`
```json
{
  "id": "guid",
  "firstName": "Ana",
  "lastName": "Lopez",
  "email": "ana.lopez@company.com",
  "phone": "5551234567",
  "dateOfBirth": "1990-06-18",
  "hireDate": "2025-01-15",
  "status": "Active",
  "addresses": [
    {
      "id": "guid",
      "addressType": "Home",
      "isPrimary": true,
      "line1": "100 Main St",
      "line2": null,
      "city": "Pittsburgh",
      "state": "PA",
      "zipCode": "15222",
      "countryCode": "US"
    },
    {
      "id": "guid",
      "addressType": "Mailing",
      "isPrimary": false,
      "line1": "200 Oak Ave",
      "line2": "Apt 4",
      "city": "Pittsburgh",
      "state": "PA",
      "zipCode": "15213",
      "countryCode": "US"
    }
  ]
}
```

### Errors
- `404` not found or not visible

## 3.3 POST `/employees`
Creates an employee and optionally one or more addresses.

### Request
```json
{
  "firstName": "Ana",
  "lastName": "Lopez",
  "email": "ana.lopez@company.com",
  "phone": "5551234567",
  "dateOfBirth": "1990-06-18",
  "hireDate": "2025-01-15",
  "status": "Active",
  "addresses": [
    {
      "addressType": "Home",
      "isPrimary": true,
      "line1": "100 Main St",
      "line2": null,
      "city": "Pittsburgh",
      "state": "PA",
      "zipCode": "15222",
      "countryCode": "US"
    },
    {
      "addressType": "Mailing",
      "isPrimary": false,
      "line1": "200 Oak Ave",
      "line2": "Apt 4",
      "city": "Pittsburgh",
      "state": "PA",
      "zipCode": "15213",
      "countryCode": "US"
    }
  ]
}
```

### Success
- Status: `201 Created`
```json
{
  "id": "guid"
}
```

### Errors
- `400` validation error
- `409` duplicate employee email
- `409` invalid primary-address rule

## 3.4 PUT `/employees/{id}`
Updates employee core fields only.

### Request
```json
{
  "firstName": "Ana",
  "lastName": "Lopez",
  "email": "ana.lopez@company.com",
  "phone": "5551234567",
  "dateOfBirth": "1990-06-18",
  "hireDate": "2025-01-15",
  "status": "Active"
}
```

### Success
- Status: `200 OK`
```json
{
  "id": "guid"
}
```

### Errors
- `400` validation error
- `404` not found or soft-deleted
- `409` duplicate employee email

## 3.5 DELETE `/employees/{id}`
Soft deletes an employee.

### Behavior
- does not physically remove the row
- preserves related addresses and audit history

### Success
- Status: `204 No Content`

### Errors
- `404` not found
- `409` already deleted if explicit conflict semantics are chosen

---

# 4. Address endpoints

## 4.1 GET `/employees/{employeeId}/addresses`
Returns employee addresses ordered primary first.

### Query parameters
| Name | Type | Notes |
|---|---|---|
| `includeDeleted` | bool? | Admin only; default false |

### Success
- Status: `200 OK`
```json
{
  "employeeId": "guid",
  "items": [
    {
      "id": "guid",
      "addressType": "Home",
      "isPrimary": true,
      "line1": "100 Main St",
      "line2": null,
      "city": "Pittsburgh",
      "state": "PA",
      "zipCode": "15222",
      "countryCode": "US"
    },
    {
      "id": "guid",
      "addressType": "Mailing",
      "isPrimary": false,
      "line1": "200 Oak Ave",
      "line2": "Apt 4",
      "city": "Pittsburgh",
      "state": "PA",
      "zipCode": "15213",
      "countryCode": "US"
    }
  ]
}
```

## 4.2 GET `/employees/{employeeId}/addresses/{addressId}`
Returns a single address.

### Success
- Status: `200 OK`
```json
{
  "id": "guid",
  "employeeId": "guid",
  "addressType": "Home",
  "isPrimary": true,
  "line1": "100 Main St",
  "line2": null,
  "city": "Pittsburgh",
  "state": "PA",
  "zipCode": "15222",
  "countryCode": "US"
}
```

### Errors
- `404` not found or not visible

## 4.3 POST `/employees/{employeeId}/addresses`
Creates an address for an employee.

### Request
```json
{
  "addressType": "Home",
  "isPrimary": true,
  "line1": "100 Main St",
  "line2": null,
  "city": "Pittsburgh",
  "state": "PA",
  "zipCode": "15222",
  "countryCode": "US"
}
```

### Success
- Status: `201 Created`
```json
{
  "id": "guid",
  "employeeId": "guid"
}
```

### Errors
- `400` validation error
- `404` employee not found or soft-deleted

## 4.4 PUT `/employees/{employeeId}/addresses/{addressId}`
Updates an address.

### Request
```json
{
  "addressType": "Mailing",
  "isPrimary": false,
  "line1": "200 Oak Ave",
  "line2": "Apt 4",
  "city": "Pittsburgh",
  "state": "PA",
  "zipCode": "15213",
  "countryCode": "US"
}
```

### Success
- Status: `200 OK`
```json
{
  "id": "guid",
  "employeeId": "guid"
}
```

### Errors
- `400` validation error
- `404` employee or address not found
- `409` invalid primary-address rule if explicit conflict semantics are chosen

## 4.5 PATCH `/employees/{employeeId}/addresses/{addressId}/primary`
Sets one address as the active primary address for any employee. Admin only.

### Request
No body required.

### Success
- Status: `204 No Content`

### Errors
- `404` employee or address not found
- `409` address is soft-deleted

## 4.6 DELETE `/employees/{employeeId}/addresses/{addressId}`
Soft deletes an address for any employee. Admin only.

### Behavior
- does not physically remove the row
- preserves history and audit trail
- if the deleted address is primary, a replacement is chosen deterministically when available

### Success
- Status: `204 No Content`

### Errors
- `404` employee or address not found
- `409` already deleted if explicit conflict semantics are chosen

---

# 5. Self-service address endpoints

These endpoints exist so linked self-service users can manage their own employee addresses without supplying or guessing their employee ID.

## 5.1 GET `/me/addresses`
Returns active addresses for the authenticated user's linked employee profile, ordered primary first.

### Success
- Status: `200 OK`
```json
{
  "employeeId": "guid",
  "items": [
    {
      "id": "guid",
      "addressType": "Home",
      "isPrimary": true,
      "line1": "100 Main St",
      "line2": null,
      "city": "Pittsburgh",
      "state": "PA",
      "zipCode": "15222",
      "countryCode": "US"
    }
  ]
}
```

### Errors
- `403` user has no linked employee profile
- `404` linked employee not found or not visible

## 5.2 PATCH `/me/addresses/{addressId}/primary`
Sets one of the authenticated user's own addresses as the active primary address.

### Request
No body required.

### Success
- Status: `204 No Content`

### Errors
- `403` address does not belong to the authenticated user's linked employee profile
- `404` address not found or not visible
- `409` address is soft-deleted

## 5.3 DELETE `/me/addresses/{addressId}`
Soft deletes one of the authenticated user's own addresses.

### Behavior
- does not physically remove the row
- preserves history and audit trail
- if the deleted address is primary, a replacement is chosen deterministically when available

### Success
- Status: `204 No Content`

### Errors
- `403` address does not belong to the authenticated user's linked employee profile
- `404` address not found or not visible
- `409` already deleted if explicit conflict semantics are chosen

---

# 6. User and role endpoints

## 6.1 GET `/roles`
Returns canonical roles from the `Role` table. Admin only.

### Query parameters
| Name | Type | Notes |
|---|---|---|
| `includeInactive` | bool? | Admin only; default false |

### Success
- Status: `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "code": "Admin",
      "name": "Administrator",
      "isActive": true,
      "isSystem": true
    },
    {
      "id": "guid",
      "code": "HR",
      "name": "Human Resources",
      "isActive": true,
      "isSystem": true
    },
    {
      "id": "guid",
      "code": "Manager",
      "name": "Manager",
      "isActive": true,
      "isSystem": true
    },
    {
      "id": "guid",
      "code": "Developer",
      "name": "Developer",
      "isActive": true,
      "isSystem": true
    }
  ]
}
```

## 6.2 GET `/users`
Returns paginated users for admin user-management, onboarding, and role-assignment screens. Admin only.

### Query parameters
| Name | Type | Notes |
|---|---|---|
| `page` | int | default 1 |
| `pageSize` | int | default 25 |
| `email` | string? | optional contains filter |
| `roleCode` | string? | optional exact role-code filter |
| `includeInactive` | bool? | Admin only; default false |

### Success
- Status: `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "email": "user@example.com",
      "roleId": "guid",
      "roleCode": "Developer",
      "roleName": "Developer",
      "employeeId": "guid or null",
      "employeeName": "Ana Lopez or null",
      "isActive": true,
      "mustChangePassword": true,
      "temporaryPasswordExpiresAtUtc": "2026-03-31T19:00:00Z",
      "lastTemporaryPasswordIssuedAtUtc": "2026-03-30T19:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 1
}
```

### Required behavior
- only Admin may call this endpoint
- active users are returned by default
- inactive users are included only when `includeInactive=true`
- results are sorted by `email ASC`
- `employeeName` is derived from the linked employee record when `employeeId` exists; otherwise `null`
- this endpoint exists so the frontend can select a trustworthy role-assignment target without inventing a manual user ID flow
- onboarding status fields are returned so Admin can track invite and activation posture

## 6.3 POST `/users`
Creates a login user separately from employee creation and sends a temporary-password onboarding email. Admin only.

### Request
```json
{
  "roleId": "guid",
  "employeeId": "guid or null",
  "email": "user@company.com or null"
}
```

### Success
- Status: `201 Created`
```json
{
  "userId": "guid",
  "email": "user@company.com",
  "roleId": "guid",
  "roleCode": "Developer",
  "employeeId": "guid or null",
  "mustChangePassword": true,
  "temporaryPasswordExpiresAtUtc": "2026-03-31T19:00:00Z"
}
```

### Errors
- `400` invalid payload
- `404` employee not found
- `404` role not found
- `409` target role is inactive
- `409` employee is already linked to another user
- `409` role/linking rule violation

### Required behavior
- only Admin may call this endpoint
- employee creation and user creation remain separate workflows
- `Manager` and `Developer` require a linked employee
- if `employeeId` is supplied, the user email is derived from the employee record and `email` must be omitted
- if `employeeId` is omitted, `email` is required and only `Admin` or `HR` may be created that way
- creation issues a new temporary password with a 24-hour expiry and sets `mustChangePassword=true`
- onboarding email is sent from the configured no-reply sender

## 6.4 POST `/users/{userId}/resend-temporary-password`
Issues a new onboarding temporary password and re-sends the invite email. Admin only.

### Request
No body required.

### Success
- Status: `200 OK`
```json
{
  "userId": "guid",
  "temporaryPasswordExpiresAtUtc": "2026-04-01T19:00:00Z"
}
```

### Errors
- `400` user is not in an onboarding state
- `404` user not found

### Required behavior
- only Admin may call this endpoint
- this flow is only for onboarding or still-unactivated accounts
- a resend invalidates any previous temporary password immediately
- active sessions for the affected user are revoked immediately
- the replacement invite email is sent from the configured no-reply sender

## 6.5 PATCH `/users/{userId}/role`
Assigns or changes the role of a user. Admin only.

### Request
```json
{
  "roleId": "guid"
}
```

### Success
- Status: `200 OK`
```json
{
  "userId": "guid",
  "roleId": "guid",
  "roleCode": "Admin",
  "sessionsRevoked": 1
}
```

### Errors
- `400` invalid payload or ambiguous role selector
- `404` user not found
- `404` role not found
- `409` target role is inactive
- `409` role change would leave zero active Admin users

### Required behavior
- only Admin may call this endpoint
- target role must exist and be active
- active sessions for the affected user are revoked immediately after a successful change
- role change must be audited

---

# 7. Audit log endpoint

## 7.1 GET `/audit-logs`
Returns paginated audit events for Admin.

### Query parameters
| Name | Type | Notes |
|---|---|---|
| `page` | int | default 1 |
| `pageSize` | int | bounded |
| `actorUserId` | guid? | optional |
| `actionType` | string? | canonical value |
| `entityType` | string? | canonical value |
| `entityId` | guid? | optional |
| `result` | string? | canonical value |
| `fromUtc` | datetime? | optional |
| `toUtc` | datetime? | optional |

### Success
- Status: `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "actorUserId": "guid",
      "sessionId": "guid",
      "actionType": "EmployeeUpdated",
      "entityType": "Employee",
      "entityId": "guid",
      "result": "Success",
      "correlationId": "req-123",
      "occurredAtUtc": "2026-03-16T17:21:43Z"
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalCount": 1
}
```

---

# 7. Validation notes

## 7.1 Employee
- `dateOfBirth` is required
- `dateOfBirth` must represent an employee who is 21 years old or older on the current date
- `hireDate` is required
- `hireDate` must not be earlier than `dateOfBirth + 14 years`

## 7.2 Address
- `addressType` is required
- `line1`, `city`, `state`, `zipCode`, and `countryCode` are required
- `isPrimary=true` must still preserve the one-primary-address rule

---

# 8. Frontend integration notes

1. Browser calls that rely on cookie authentication must send credentials.
2. UTC date-time values must be rendered by the frontend in local time.
3. Date-only values such as `dateOfBirth` and `hireDate` must be displayed as date-only values without timezone conversion.
4. Frontend should use `GET /auth/session` as the bootstrap source for current user identity and session timing after login and on page reload.
5. Frontend should call `GET /auth/antiforgery` after login/session bootstrap and send `X-CSRF-TOKEN` for all state-changing requests.
6. Frontend should renew only while the current session is still valid; a `401` means the user must log in again.
7. The role-assignment screen should combine `GET /roles` for the canonical role catalog with `GET /users` for the selectable target-user list.
