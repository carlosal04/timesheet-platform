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

interface AssignUserRoleResponse {
  userId: string
  roleId: string
  roleCode: string
  sessionsRevoked: number
}

export interface RoleAssignmentResult {
  userId: string
  roleId: string
  roleCode: string
  sessionsRevoked: number
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
