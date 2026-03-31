---
title: Employment Management System (EMS) — Phase 1 Roles and Permissions Matrix
version: 1.4-draft
status: Review Pending
---

# 1. Roles overview

## Admin
Full-access internal EMS role for platform administration, user provisioning, security-sensitive access changes, and audit review.

Capabilities:
- full employee and address administration
- user provisioning and onboarding resend
- role assignment
- audit-log access

## HR
Operational internal role for employee and address administration without user/role or audit administration.

Capabilities:
- read employee data
- create employee data
- update employee data
- soft delete employee data
- manage addresses for any employee
- change the primary address for any employee

## Manager
Operational role for employee-directory visibility plus self-service address actions on the linked employee profile.

Capabilities:
- read employee data
- view addresses for visible employees
- soft delete own addresses
- change own primary address

## Developer
Restricted self-service role for the linked employee profile only.

Capabilities:
- view own linked employee addresses
- soft delete own addresses
- change own primary address

The word "own" means the address belongs to the employee record linked to `User.EmployeeId`.

---

# 2. Permissions matrix

| Action | Description | Policy | Admin | HR | Manager | Developer |
|---|---|---|:---:|:---:|:---:|:---:|
| Login | Authenticate using email + password | Anonymous | ✔️ | ✔️ | ✔️ | ✔️ |
| Logout | Revoke current session | `AuthenticatedUser` | ✔️ | ✔️ | ✔️ | ✔️ |
| Session Renew | Extend a still-valid session | `AuthenticatedUser` | ✔️ | ✔️ | ✔️ | ✔️ |
| Change Password | Change current or temporary password | `AuthenticatedUser` | ✔️ | ✔️ | ✔️ | ✔️ |
| Forgot Password | Start self-service reset | Anonymous | ✔️ | ✔️ | ✔️ | ✔️ |
| Reset Password | Complete self-service reset | Anonymous | ✔️ | ✔️ | ✔️ | ✔️ |
| List Employees | View paginated employee list | `EmployeeRead` | ✔️ | ✔️ | ✔️ | ❌ |
| View Employee Details | View employee and active addresses | `EmployeeRead` | ✔️ | ✔️ | ✔️ | ❌ |
| Create Employee | Add a new employee | `EmployeeWrite` | ✔️ | ✔️ | ❌ | ❌ |
| Update Employee | Modify employee fields | `EmployeeWrite` | ✔️ | ✔️ | ❌ | ❌ |
| Soft Delete Employee | Mark employee as deleted | `EmployeeDelete` | ✔️ | ✔️ | ❌ | ❌ |
| List Employee Addresses | View active addresses for an employee | `AddressRead` | ✔️ | ✔️ | ✔️ | ❌ |
| View Employee Address | View a single address | `AddressRead` | ✔️ | ✔️ | ✔️ | ❌ |
| Create Address | Add a new address | `AddressWrite` | ✔️ | ✔️ | ❌ | ❌ |
| Update Address | Edit an address | `AddressWrite` | ✔️ | ✔️ | ❌ | ❌ |
| Set Primary Address (Any Employee) | Make any employee address primary | `AddressPrimaryManageAny` | ✔️ | ✔️ | ❌ | ❌ |
| List Own Addresses | View own linked employee addresses | `OwnAddressRead` | ❌ | ❌ | ✔️ | ✔️ |
| Set Primary Address (Own) | Make own linked employee address primary | `OwnAddressPrimaryManage` | ❌ | ❌ | ✔️ | ✔️ |
| Soft Delete Address (Any Employee) | Mark any employee address as deleted | `AddressDeleteAny` | ✔️ | ✔️ | ❌ | ❌ |
| Soft Delete Address (Own) | Mark own linked employee address as deleted | `OwnAddressDelete` | ❌ | ❌ | ✔️ | ✔️ |
| List Roles | View canonical roles from the `Role` table | `RoleRead` | ✔️ | ❌ | ❌ | ❌ |
| List Users | View user list and onboarding state | `UserRead` | ✔️ | ❌ | ❌ | ❌ |
| Create User | Create a user and send onboarding email | `UserCreate` | ✔️ | ❌ | ❌ | ❌ |
| Resend Temporary Password | Reissue onboarding credential | `UserResendTemporaryPassword` | ✔️ | ❌ | ❌ | ❌ |
| Assign User Role | Assign or change a user's role | `UserRoleAssign` | ✔️ | ❌ | ❌ | ❌ |
| View Audit Logs | View system audit entries | `AuditLogRead` | ✔️ | ❌ | ❌ | ❌ |

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
- `Manager` users must never request or view soft-deleted employees
- `Manager` and `Developer` users must never request or view soft-deleted addresses
- Admin-only deleted-data visibility must be explicit and intentional

## 3.5 Role and onboarding administration
- only Admin can list roles
- only Admin can list users
- only Admin can create users or resend onboarding temporary passwords
- only Admin can assign or change a user role
- role assignment and onboarding are security-sensitive operations and must use explicit policy protection

---

# 4. Soft delete rules

## 4.1 Employee
- only Admin and HR can soft delete an employee
- employee delete is soft delete only in Phase 1
- soft-deleted employees are hidden by default

## 4.2 Address
- Admin and HR can soft delete any address
- Manager and Developer can soft delete only an address that belongs to the employee linked to `User.EmployeeId`
- address delete is soft delete only in Phase 1
- soft-deleted addresses are hidden by default

---

# 5. Primary address rules

1. Admin and HR can assign or change the primary address for any employee.
2. Manager and Developer can assign or change the primary address only for the employee linked to `User.EmployeeId`.
3. An employee may have zero or one active primary address.
4. Primary address changes must be transactional.
5. Address collections must be returned with the primary address first.

---

# 6. Role-table rules

1. Authorization must use the canonical role codes stored in the `Role` table.
2. The canonical Phase 1 role codes are `Admin`, `HR`, `Manager`, and `Developer`.
3. Role code changes are security-relevant and must trigger session revocation for affected users.
4. Only active roles are assignable.
5. Inactive roles may be visible to Admin but must not be accepted by role-assignment or user-creation commands.
6. The application must not allow a role change that would leave the system with zero active users assigned to the `Admin` role.
7. Phase 1 does not expose public role CRUD endpoints; only role list and Admin-controlled user/role actions are exposed.

---

# 7. User-linking rules

1. `Manager` and `Developer` require a non-null `EmployeeId`.
2. `Admin` and `HR` may be created with no linked employee.
3. If a user is created with `EmployeeId`, the user email is derived from the employee record.
4. An employee may be linked to at most one user.
5. Self-service routes use `User.EmployeeId` as the ownership link.

---

# 8. Audit access rules

1. Only Admin can read audit logs.
2. No user-facing role can modify or delete audit entries through the application.
