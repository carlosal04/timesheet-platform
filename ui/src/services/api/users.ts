import { apiRequest } from '@/services/api/http'
import type { UserListRow } from '@/types/ems'

interface UsersResponse {
  items: UserItem[]
  page: number
  pageSize: number
  totalCount: number
}

interface UserItem {
  id: string
  email: string
  roleId: string
  roleCode: string
  roleName: string
  employeeId: string | null
  employeeName: string | null
  isActive: boolean
  mustChangePassword: boolean
  temporaryPasswordExpiresAtUtc: string | null
  lastTemporaryPasswordIssuedAtUtc: string | null
}

export interface UserListQuery {
  page: number
  pageSize: number
  email?: string
  roleCode?: string
  includeInactive?: boolean
}

export interface UserListPage {
  items: UserListRow[]
  page: number
  pageSize: number
  totalCount: number
}

interface CreateUserResponse {
  userId: string
  email: string
  roleId: string
  roleCode: string
  employeeId: string | null
  mustChangePassword: boolean
  temporaryPasswordExpiresAtUtc: string
}

export interface CreateUserInput {
  roleId: string
  employeeId?: string
  email?: string
}

export interface CreateUserResult {
  userId: string
  email: string
  roleId: string
  roleCode: string
  employeeId: string | null
  mustChangePassword: boolean
  temporaryPasswordExpiresAtUtc: string
}

interface AssignUserRoleResponse {
  userId: string
  roleId: string
  roleCode: string
  sessionsRevoked: number
}

interface ResendTemporaryPasswordResponse {
  userId: string
  temporaryPasswordExpiresAtUtc: string
}

export interface RoleAssignmentResult {
  userId: string
  roleId: string
  roleCode: string
  sessionsRevoked: number
}

export interface ResendTemporaryPasswordResult {
  userId: string
  temporaryPasswordExpiresAtUtc: string
}

function toQueryString(query: UserListQuery) {
  const params = new URLSearchParams()

  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))

  if (query.email) {
    params.set('email', query.email)
  }

  if (query.roleCode) {
    params.set('roleCode', query.roleCode)
  }

  if (query.includeInactive) {
    params.set('includeInactive', 'true')
  }

  return params.toString()
}

function toUserListRow(item: UserItem): UserListRow {
  return {
    id: item.id,
    email: item.email,
    roleId: item.roleId,
    roleCode: item.roleCode,
    roleName: item.roleName,
    employeeId: item.employeeId,
    employeeName: item.employeeName,
    isActive: item.isActive,
    mustChangePassword: item.mustChangePassword,
    temporaryPasswordExpiresAtUtc: item.temporaryPasswordExpiresAtUtc,
    lastTemporaryPasswordIssuedAtUtc: item.lastTemporaryPasswordIssuedAtUtc,
  }
}

export async function listUsers(query: UserListQuery): Promise<UserListPage> {
  const queryString = toQueryString(query)
  const response = await apiRequest<UsersResponse>(`/users?${queryString}`)

  return {
    items: response.items.map(toUserListRow),
    page: response.page,
    pageSize: response.pageSize,
    totalCount: response.totalCount,
  }
}

export async function createUser(input: CreateUserInput): Promise<CreateUserResult> {
  const response = await apiRequest<CreateUserResponse>('/users', {
    method: 'POST',
    body: {
      roleId: input.roleId,
      employeeId: input.employeeId ?? null,
      email: input.email ?? null,
    },
  })

  return {
    userId: response.userId,
    email: response.email,
    roleId: response.roleId,
    roleCode: response.roleCode,
    employeeId: response.employeeId,
    mustChangePassword: response.mustChangePassword,
    temporaryPasswordExpiresAtUtc: response.temporaryPasswordExpiresAtUtc,
  }
}

export async function assignUserRole(userId: string, roleId: string): Promise<RoleAssignmentResult> {
  const response = await apiRequest<AssignUserRoleResponse>(`/users/${userId}/role`, {
    method: 'PATCH',
    body: { roleId },
  })

  return {
    userId: response.userId,
    roleId: response.roleId,
    roleCode: response.roleCode,
    sessionsRevoked: response.sessionsRevoked,
  }
}

export async function resendTemporaryPassword(userId: string): Promise<ResendTemporaryPasswordResult> {
  const response = await apiRequest<ResendTemporaryPasswordResponse>(`/users/${userId}/resend-temporary-password`, {
    method: 'POST',
  })

  return {
    userId: response.userId,
    temporaryPasswordExpiresAtUtc: response.temporaryPasswordExpiresAtUtc,
  }
}
