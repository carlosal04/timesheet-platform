export type RoleCode = 'Admin' | 'Basic'

export interface EmployeeAddress {
  id: string
  addressType: 'Home' | 'Mailing' | 'Work'
  isPrimary: boolean
  line1: string
  line2: string | null
  city: string
  state: string
  zipCode: string
  countryCode: string
  createdAtUtc?: string
  deletedAtUtc?: string | null
}

export interface EmployeeRecord {
  id: string
  firstName: string
  lastName: string
  email: string
  phone: string
  dateOfBirth: string
  hireDate: string
  status: 'Active' | 'Inactive'
  addresses: EmployeeAddress[]
}

export interface UserRecord {
  id: string
  name: string
  email: string
  roleCode: RoleCode
  employeeId: string | null
}

export interface RoleRecord {
  id: string
  code: string
  name: string
  isActive: boolean
  isSystem?: boolean
}

export interface UserListRow {
  id: string
  email: string
  roleId: string
  roleCode: string
  roleName: string
  employeeId: string | null
  employeeName: string | null
  isActive: boolean
}

export interface AuditRecord {
  id: string
  occurredAtUtc: string
  actionType: string
  actorEmail: string
  entityType: string
  summary: string
}

export interface SessionSnapshot {
  userId: string
  email: string
  roleCode: RoleCode
  employeeId: string | null
  sessionId: string
  expiresAtUtc: string
  idleTimeoutMinutes: number
}

export interface EmployeeListRow {
  id: string
  firstName: string
  lastName: string
  email: string
  phone: string
  dateOfBirth: string
  hireDate: string
  status: EmployeeRecord['status']
  primaryAddress: EmployeeAddress | null
}

export interface LoginProblem {
  status: number
  title: string
  detail: string
}
