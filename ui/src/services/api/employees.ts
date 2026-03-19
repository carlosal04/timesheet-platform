import type { EmployeeAddress, EmployeeListRow } from '@/types/ems'
import { apiRequest } from '@/services/api/http'

interface EmployeeListResponse {
  items: EmployeeListItem[]
  page: number
  pageSize: number
  totalCount: number
}

interface EmployeeListItem {
  id: string
  firstName: string
  lastName: string
  email: string
  phone: string
  dateOfBirth: string
  hireDate: string
  status: EmployeeListRow['status']
  primaryAddress: EmployeeListPrimaryAddress | null
}

interface EmployeeListPrimaryAddress {
  id: string
  addressType: EmployeeAddress['addressType']
  isPrimary: boolean
  line1: string
  line2: string | null
  city: string
  state: string
  zipCode: string
  countryCode: string
}

export interface EmployeeListQuery {
  page: number
  pageSize: number
  name?: string
  status?: string
  hireDateFrom?: string
  hireDateTo?: string
  includeDeleted?: boolean
  includePrimaryAddress?: boolean
}

export interface EmployeeListPage {
  items: EmployeeListRow[]
  page: number
  pageSize: number
  totalCount: number
}

function toQueryString(query: EmployeeListQuery) {
  const params = new URLSearchParams()

  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))

  if (query.name) {
    params.set('name', query.name)
  }

  if (query.status) {
    params.set('status', query.status)
  }

  if (query.hireDateFrom) {
    params.set('hireDateFrom', query.hireDateFrom)
  }

  if (query.hireDateTo) {
    params.set('hireDateTo', query.hireDateTo)
  }

  if (query.includeDeleted) {
    params.set('includeDeleted', 'true')
  }

  if (query.includePrimaryAddress) {
    params.set('includePrimaryAddress', 'true')
  }

  return params.toString()
}

function toEmployeeRow(item: EmployeeListItem): EmployeeListRow {
  return {
    id: item.id,
    firstName: item.firstName,
    lastName: item.lastName,
    email: item.email,
    phone: item.phone,
    dateOfBirth: item.dateOfBirth,
    hireDate: item.hireDate,
    status: item.status,
    primaryAddress: item.primaryAddress,
  }
}

export async function listEmployees(query: EmployeeListQuery): Promise<EmployeeListPage> {
  const queryString = toQueryString(query)
  const response = await apiRequest<EmployeeListResponse>(`/employees?${queryString}`)

  return {
    items: response.items.map(toEmployeeRow),
    page: response.page,
    pageSize: response.pageSize,
    totalCount: response.totalCount,
  }
}
