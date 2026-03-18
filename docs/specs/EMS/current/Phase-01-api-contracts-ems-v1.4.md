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
  "roleCode": "Admin"
}
```

### Errors
- `400` invalid payload
- `401` invalid credentials
- `423` locked out
- `429` too many attempts

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

These endpoints exist so Basic users can manage their own linked employee addresses without supplying or guessing their employee ID.

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

# 6. Role and user-role endpoints

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
      "code": "Basic",
      "name": "Basic User",
      "isActive": true,
      "isSystem": true
    }
  ]
}
```

## 6.2 PATCH `/users/{userId}/role`
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
