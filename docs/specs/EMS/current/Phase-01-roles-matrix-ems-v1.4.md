---
title: Employment Management System (EMS) — Phase 1 Roles and Permissions Matrix
version: 1.4-draft
status: Review Pending
---

# 1. Roles overview

## Admin
Full-access internal role for approved HR and administrative operations users.

Capabilities:
- read employee data
- create employee data
- update employee data
- soft delete employee data
- manage addresses for any employee
- change the primary address for any employee
- view audit logs
- list canonical roles
- assign or change any user role

## Basic
Limited internal role for staff who need employee visibility and restricted self-service address actions.

Capabilities:
- read employee data
- view addresses
- soft delete own addresses
- change own primary address

The word "own" means the address belongs to the employee record linked to `User.EmployeeId`.

---

# 2. Permissions matrix

| Action | Description | Policy | Admin | Basic |
|---|---|---|:---:|:---:|
| Login | Authenticate using email + password | Anonymous | ✔️ | ✔️ |
| Logout | Revoke current session | `AuthenticatedUser` | ✔️ | ✔️ |
| List Employees | View paginated employee list | `EmployeeRead` | ✔️ | ✔️ |
| View Employee Details | View employee and active addresses | `EmployeeRead` | ✔️ | ✔️ |
| Create Employee | Add a new employee | `EmployeeWrite` | ✔️ | ❌ |
| Update Employee | Modify employee fields | `EmployeeWrite` | ✔️ | ❌ |
| Soft Delete Employee | Mark employee as deleted | `EmployeeDelete` | ✔️ | ❌ |
| List Employee Addresses | View active addresses for an employee | `AddressRead` | ✔️ | ✔️ |
| View Employee Address | View a single address | `AddressRead` | ✔️ | ✔️ |
| Create Address | Add a new address | `AddressWrite` | ✔️ | ❌ |
| Update Address | Edit an address | `AddressWrite` | ✔️ | ❌ |
| Set Primary Address (Any Employee) | Make any employee address primary | `AddressPrimaryManageAny` | ✔️ | ❌ |
| Set Primary Address (Own) | Make own linked employee address primary | `OwnAddressPrimaryManage` | ❌ | ✔️ |
| Soft Delete Address (Any Employee) | Mark any employee address as deleted | `AddressDeleteAny` | ✔️ | ❌ |
| Soft Delete Address (Own) | Mark own linked employee address as deleted | `OwnAddressDelete` | ❌ | ✔️ |
| List Roles | View canonical roles from the `Role` table | `RoleRead` | ✔️ | ❌ |
| Assign User Role | Assign or change a user's role | `UserRoleAssign` | ✔️ | ❌ |
| View Audit Logs | View system audit entries | `AuditLogRead` | ✔️ | ❌ |

---

# 3. Authorization rules

## 3.1 Canonical enforcement model
Authorization is enforced primarily at the API boundary using named ASP.NET Core policies.

## 3.2 Deny-by-default rule
All endpoints are protected by default unless explicitly marked anonymous.

## 3.3 Handler guidance
Handlers must not reimplement static role checks already enforced by policies.

Handlers may participate in contextual authorization only when a rule depends on resource ownership or resource state.

## 3.4 Deleted-data visibility
- Basic users must never request or view soft-deleted employees
- Basic users must never request or view soft-deleted addresses
- Admin-only deleted-data visibility must be explicit and intentional

## 3.5 Role assignment authorization
- only Admin can list roles
- only Admin can assign or change a user role
- role assignment is a security-sensitive operation and must use explicit policy protection

---

# 4. Soft delete rules

## 4.1 Employee
- only Admin can soft delete an employee
- employee delete is soft delete only in Phase 1
- soft-deleted employees are hidden by default

## 4.2 Address
- Admin can soft delete any address
- Basic can soft delete only an address that belongs to the employee linked to `User.EmployeeId`
- address delete is soft delete only in Phase 1
- soft-deleted addresses are hidden by default

---

# 5. Primary address rules

1. Admin can assign or change the primary address for any employee.
2. Basic can assign or change the primary address only for the employee linked to `User.EmployeeId`.
3. An employee may have zero or one active primary address.
4. Primary address changes must be transactional.
5. Address collections must be returned with the primary address first.

---

# 6. Role-table rules

1. Authorization must use the canonical role codes stored in the `Role` table.
2. The application must not rely on ad hoc freeform role names such as `Developer`, `Dev`, or inconsistent casing.
3. The canonical Phase 1 role codes are `Admin` and `Basic`.
4. Role code changes are security-relevant and must trigger session revocation for affected users.
5. Only active roles are assignable.
6. Inactive roles may be visible to Admin but must not be accepted by role-assignment commands.
7. The application must not allow a role change that would leave the system with zero active users assigned to the `Admin` role.
8. Phase 1 does not expose public role CRUD endpoints; only role list and user-role assignment are exposed.

---

# 7. Audit access rules

1. Only Admin can read audit logs.
2. No user-facing role can modify or delete audit entries through the application.
