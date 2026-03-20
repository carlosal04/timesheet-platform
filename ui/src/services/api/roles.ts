import { apiRequest } from '@/services/api/http'
import type { RoleRecord } from '@/types/ems'

interface RolesResponse {
  items: RoleItem[]
}

interface RoleItem {
  id: string
  code: RoleRecord['code']
  name: string
  isActive: boolean
  isSystem: boolean
}

export async function listRoles(includeInactive = false): Promise<RoleItem[]> {
  const query = includeInactive ? '?includeInactive=true' : ''
  const response = await apiRequest<RolesResponse>(`/roles${query}`)
  return response.items
}
