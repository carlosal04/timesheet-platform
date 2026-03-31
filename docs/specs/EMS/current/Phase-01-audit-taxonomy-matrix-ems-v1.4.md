---
title: Employment Management System (EMS) — Phase 1 Audit Taxonomy Matrix
version: 1.4-draft
status: Review Pending
---

# 1. Purpose

This document is the canonical source for the allowed audit taxonomy values in Phase 1.

It defines:
- action types
- entity types
- result values

Any implementation, code generator, or later documentation must reuse these values exactly unless a future approved version changes them.

---

# 2. Canonical result values

| Result | Meaning |
|---|---|
| `Success` | Operation completed successfully |
| `Failure` | Operation failed after evaluation or execution |
| `Denied` | Request was rejected due to authorization or security policy |
| `Rejected` | Request was rejected due to validation, lockout, or state rules |
| `NotFound` | Target entity was not found or not visible |
| `Conflict` | Request conflicted with current state |

---

# 3. Canonical entity types

| EntityType | Meaning |
|---|---|
| `Authentication` | Login/logout/auth-cookie/session related events |
| `UserSession` | Server-side session lifecycle |
| `User` | Internal EMS user or account state |
| `Employee` | Employee master record |
| `EmployeeAddress` | Employee address record |
| `AuditLog` | Audit subsystem events if needed |
| `System` | Startup/configuration or global operational event |

---

# 4. Canonical action types

## 4.1 Authentication and session actions
| ActionType | Primary EntityType | Typical Result Values |
|---|---|---|
| `LoginSucceeded` | `Authentication` | `Success` |
| `LoginFailed` | `Authentication` | `Failure`, `Rejected` |
| `LogoutSucceeded` | `Authentication` | `Success` |
| `SessionCreated` | `UserSession` | `Success` |
| `SessionRevoked` | `UserSession` | `Success` |
| `AccessDenied` | `Authentication` | `Denied` |
| `AccountLockedOut` | `User` | `Rejected`, `Success` |
| `AccountDisabled` | `User` | `Success` |
| `UserCreated` | `User` | `Success`, `Conflict`, `NotFound`, `Rejected` |
| `TemporaryPasswordIssued` | `User` | `Success`, `Conflict`, `NotFound`, `Rejected` |
| `TemporaryPasswordResent` | `User` | `Success`, `Conflict`, `NotFound`, `Rejected` |
| `PasswordResetRequested` | `User` | `Success` |
| `UserRoleAssigned` | `User` | `Success`, `Conflict`, `NotFound`, `Denied` |
| `RoleAssignmentRejected` | `User` | `Rejected`, `Conflict`, `Denied`, `NotFound` |
| `PasswordChanged` | `User` | `Success` |

## 4.2 Employee actions
| ActionType | Primary EntityType | Typical Result Values |
|---|---|---|
| `EmployeeCreated` | `Employee` | `Success`, `Failure`, `Conflict` |
| `EmployeeRead` | `Employee` | `Success`, `NotFound`, `Denied` |
| `EmployeeListRead` | `Employee` | `Success`, `Denied` |
| `EmployeeUpdated` | `Employee` | `Success`, `Failure`, `Conflict`, `NotFound` |
| `EmployeeSoftDeleted` | `Employee` | `Success`, `Conflict`, `NotFound` |

## 4.3 Address actions
| ActionType | Primary EntityType | Typical Result Values |
|---|---|---|
| `AddressCreated` | `EmployeeAddress` | `Success`, `Failure`, `Conflict`, `NotFound` |
| `AddressRead` | `EmployeeAddress` | `Success`, `NotFound`, `Denied` |
| `AddressListRead` | `EmployeeAddress` | `Success`, `Denied` |
| `AddressUpdated` | `EmployeeAddress` | `Success`, `Failure`, `Conflict`, `NotFound` |
| `AddressPrimaryChanged` | `EmployeeAddress` | `Success`, `Conflict`, `NotFound`, `Denied` |
| `AddressSoftDeleted` | `EmployeeAddress` | `Success`, `Conflict`, `NotFound`, `Denied` |

## 4.4 Audit and system actions
| ActionType | Primary EntityType | Typical Result Values |
|---|---|---|
| `AuditLogRead` | `AuditLog` | `Success`, `Denied` |
| `ConfigurationError` | `System` | `Failure` |
| `UnhandledException` | `System` | `Failure` |

---

# 5. Required audit payload guidance

Every audit record should capture:
- `ActionType`
- `EntityType`
- `EntityId` when relevant
- `Result`
- `ActorUserId` when available
- `SessionId` when available
- `OccurredAtUtc` in ISO 8601 UTC format with trailing `Z`
- `CorrelationId`
- metadata useful for investigation without storing secrets

Suggested metadata examples:
- revoked reason
- changed field names
- old/new primary-address ID
- ownership-evaluation result for self-service address actions
- validation summary code, not raw secret data

---

# 6. Usage rules

1. Use the canonical strings exactly as written.
2. Do not invent synonyms such as `EmployeeDeleted` when the approved value is `EmployeeSoftDeleted`.
3. Self-service and admin operations on the same entity still use the same canonical action name.
4. Result values should be chosen based on the final observed outcome of the attempted operation.

---

# 7. Minimum required audited events in Phase 1

The implementation must at minimum audit:
- `LoginSucceeded`
- `LoginFailed`
- `LogoutSucceeded`
- `SessionCreated`
- `SessionRevoked`
- `AccessDenied` when explicitly captured
- `EmployeeCreated`
- `EmployeeUpdated`
- `EmployeeSoftDeleted`
- `AddressCreated`
- `AddressUpdated`
- `AddressPrimaryChanged`
- `AddressSoftDeleted`
- `UserRoleAssigned`
- `AuditLogRead`

---

# 8. Future extension rule

When EMS expands into the Time Sheet platform, new audit values must be added by extending this document rather than creating competing action-name vocabularies.
