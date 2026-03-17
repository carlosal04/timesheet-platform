---
title: Employment Management System (EMS) — Phase 1 ERD
version: 1.4-draft
status: Review Pending
---

# 1. Entity list

- Role
- User
- UserSession
- Employee
- EmployeeAddress
- AuditLog

---

# 2. Text-based ERD

```text
+----------------+        1:N       +----------------+
|      Role      |-------------------|      User      |
+----------------+                   +----------------+
| Id (PK)        |                   | Id (PK)        |
| Code           |                   | Email          |
| Name           |                   | PasswordHash   |
| IsActive       |                   | RoleId (FK)    |
| CreatedAtUtc   |                   | EmployeeId (FK)|
| UpdatedAtUtc   |                   | IsActive       |
+----------------+                   | AccessFailedCt |
                                     | LockoutEndUtc  |
                                     | SessionVersion |
                                     | CreatedAtUtc   |
                                     | UpdatedAtUtc   |
                                     +----------------+

+----------------+        1:N       +------------------+
|      User      |-------------------|   UserSession    |
+----------------+                   +------------------+
| Id (PK)        |                   | Id (PK)          |
| ...            |                   | UserId (FK)      |
+----------------+                   | SessionVersion   |
                                     | CreatedAtUtc     |
                                     | LastSeenAtUtc    |
                                     | ExpiresAtUtc     |
                                     | RevokedAtUtc     |
                                     | RevokedReason    |
                                     | ClientFpHash     |
                                     | CreatedByIpHash  |
                                     +------------------+

+----------------+       0:1        +----------------+
|      User      |-------------------|    Employee     |
+----------------+                   +----------------+
| EmployeeId (FK)|                   | Id (PK)        |
+----------------+                   | FirstName      |
                                     | LastName       |
                                     | Email          |
                                     | Phone          |
                                     | DateOfBirth    |
                                     | HireDate       |
                                     | Status         |
                                     | IsDeleted      |
                                     | DeletedAtUtc   |
                                     | DeletedByUserId|
                                     | CreatedAtUtc   |
                                     | UpdatedAtUtc   |
                                     +----------------+

+----------------+        1:N       +----------------------+
|    Employee    |-------------------|   EmployeeAddress    |
+----------------+                   +----------------------+
| Id (PK)        |                   | Id (PK)              |
| ...            |                   | EmployeeId (FK)      |
+----------------+                   | AddressType          |
                                     | IsPrimary            |
                                     | Line1                |
                                     | Line2                |
                                     | City                 |
                                     | State                |
                                     | ZipCode              |
                                     | CountryCode          |
                                     | IsDeleted            |
                                     | DeletedAtUtc         |
                                     | DeletedByUserId      |
                                     | CreatedAtUtc         |
                                     | UpdatedAtUtc         |
                                     +----------------------+

+----------------+        1:N       +------------------+
|      User      |-------------------|     AuditLog     |
+----------------+                   +------------------+
| Id (PK)        |                   | Id (PK)          |
| ...            |                   | ActorUserId (FK) |
+----------------+                   | SessionId (FK)   |
                                     | ActionType       |
                                     | EntityType       |
                                     | EntityId         |
                                     | Result           |
                                     | CorrelationId    |
                                     | OccurredAtUtc    |
                                     | MetadataJson     |
                                     +------------------+
```

---

# 3. Entity details

## 3.1 Role
Purpose:
- canonical role vocabulary and referential integrity for authorization

Suggested fields:
- `Id : Guid`
- `Code : string`
- `Name : string`
- `IsActive : bool`
- `IsSystem : bool`
- `CreatedAtUtc : DateTimeOffset`
- `UpdatedAtUtc : DateTimeOffset`

Seeded canonical role codes:
- `Admin`
- `Basic`

## 3.2 User
Purpose:
- internal identity for EMS access

Suggested fields:
- `Id : Guid`
- `Email : string`
- `PasswordHash : string`
- `RoleId : Guid`
- `EmployeeId : Guid?`
- `IsActive : bool`
- `AccessFailedCount : int`
- `LockoutEndUtc : DateTimeOffset?`
- `SessionVersion : int`
- `CreatedAtUtc : DateTimeOffset`
- `UpdatedAtUtc : DateTimeOffset`

## 3.3 UserSession
Purpose:
- server-side session authority for one-active-session enforcement

Suggested fields:
- `Id : Guid`
- `UserId : Guid`
- `SessionVersion : int`
- `CreatedAtUtc : DateTimeOffset`
- `LastSeenAtUtc : DateTimeOffset`
- `ExpiresAtUtc : DateTimeOffset`
- `RevokedAtUtc : DateTimeOffset?`
- `RevokedReason : string?`
- `ClientFpHash : string?`
- `CreatedByIpHash : string?`

## 3.4 Employee
Purpose:
- employee master record

Suggested fields:
- `Id : Guid`
- `FirstName : string`
- `LastName : string`
- `Email : string`
- `Phone : string?`
- `DateOfBirth : DateOnly`
- `HireDate : DateOnly`
- `Status : string`
- `IsDeleted : bool`
- `DeletedAtUtc : DateTimeOffset?`
- `DeletedByUserId : Guid?`
- `CreatedAtUtc : DateTimeOffset`
- `UpdatedAtUtc : DateTimeOffset`

## 3.5 EmployeeAddress
Purpose:
- multiple addresses per employee, including one optional primary address

Suggested fields:
- `Id : Guid`
- `EmployeeId : Guid`
- `AddressType : string`
- `IsPrimary : bool`
- `Line1 : string`
- `Line2 : string?`
- `City : string`
- `State : string`
- `ZipCode : string`
- `CountryCode : string`
- `IsDeleted : bool`
- `DeletedAtUtc : DateTimeOffset?`
- `DeletedByUserId : Guid?`
- `CreatedAtUtc : DateTimeOffset`
- `UpdatedAtUtc : DateTimeOffset`

## 3.6 AuditLog
Purpose:
- append-only audit trail for security and business actions

Suggested fields:
- `Id : Guid`
- `ActorUserId : Guid?`
- `SessionId : Guid?`
- `ActionType : string`
- `EntityType : string`
- `EntityId : Guid?`
- `Result : string`
- `CorrelationId : string`
- `OccurredAtUtc : DateTimeOffset`
- `MetadataJson : string?`

---

# 4. Relationships

1. `Role 1:N User`
2. `User 1:N UserSession`
3. `User 0:1 Employee` through `User.EmployeeId`
4. `Employee 1:N EmployeeAddress`
5. `User 1:N AuditLog` as actor
6. `UserSession 1:N AuditLog` as session context when available

---

# 5. Required constraints

## 5.1 Role
- unique index on `Code`
- optional index on `IsActive`
- optional index on `IsSystem`

## 5.2 User
- unique index on `Email`
- foreign key to `Role`
- filtered unique index on `EmployeeId` where not null
- optional index on `IsActive`

## 5.3 UserSession
- index on `UserId`
- index on `(UserId, RevokedAtUtc, ExpiresAtUtc)`
- optional filtered uniqueness approach for one active session if supported by implementation strategy

## 5.4 Employee
- unique index on `Email`
- index on `IsDeleted`
- index on `Status`
- check or domain validation for required names and valid dates

## 5.5 EmployeeAddress
- index on `EmployeeId`
- index on `IsDeleted`
- filtered unique index to enforce one active primary address per employee, conceptually:

```text
UNIQUE (EmployeeId) WHERE IsPrimary = true AND IsDeleted = false
```

## 5.6 AuditLog
- index on `OccurredAtUtc`
- index on `ActorUserId`
- index on `EntityType, EntityId`
- index on `ActionType, Result`

---

# 6. Domain enums / canonical value sets

## 6.1 Role codes
- `Admin`
- `Basic`

Role-state rules:
- only active roles are assignable
- seeded system roles should be protected from accidental deletion or destructive mutation

## 6.2 Employee status
Suggested initial values:
- `Active`
- `Inactive`

## 6.3 Address type
- `Home`
- `Mailing`
- `EmergencyContact`
- `Other`

## 6.4 Audit values
Canonical values must come from the dedicated audit taxonomy matrix document.

---

# 7. Soft delete visibility rules

1. Employees with `IsDeleted = true` are hidden by default.
2. Addresses with `IsDeleted = true` are hidden by default.
3. Soft-deleted employees remain linked to their addresses and audit logs.
4. Soft-deleted addresses remain linked to their employee and audit logs.

---

# 8. Ordering rules

## 8.1 Address ordering
Whenever a collection of employee addresses is returned, ordering must be:
1. active primary address first
2. other active addresses by `CreatedAtUtc ASC`
3. deleted addresses omitted unless explicitly requested by an Admin endpoint/filter
