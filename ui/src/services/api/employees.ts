import type { EmployeeAddress, EmployeeListRow, EmployeeRecord } from '@/types/ems'
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

interface EmployeeDetailResponse {
  id: string
  firstName: string
  lastName: string
  email: string
  phone: string
  dateOfBirth: string
  hireDate: string
  status: EmployeeRecord['status']
  addresses: EmployeeDetailAddress[]
}

interface EmployeeDetailAddress {
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

export interface EmployeeWriteInput {
  firstName: string
  lastName: string
  email: string
  phone: string
  dateOfBirth: string
  hireDate: string
  status: EmployeeRecord['status']
}

interface IdResponse {
  id: string
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

function toEmployeeRecord(response: EmployeeDetailResponse): EmployeeRecord {
  return {
    id: response.id,
    firstName: response.firstName,
    lastName: response.lastName,
    email: response.email,
    phone: response.phone,
    dateOfBirth: response.dateOfBirth,
    hireDate: response.hireDate,
    status: response.status,
    addresses: response.addresses,
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

export async function getEmployeeById(employeeId: string): Promise<EmployeeRecord> {
  const response = await apiRequest<EmployeeDetailResponse>(`/employees/${employeeId}`)
  return toEmployeeRecord(response)
}

export async function createEmployee(input: EmployeeWriteInput): Promise<string> {
  const response = await apiRequest<IdResponse>('/employees', {
    method: 'POST',
    body: {
      firstName: input.firstName,
      lastName: input.lastName,
      email: input.email,
      phone: input.phone,
      dateOfBirth: input.dateOfBirth,
      hireDate: input.hireDate,
      status: input.status,
    },
  })

  return response.id
}

export async function updateEmployee(employeeId: string, input: EmployeeWriteInput): Promise<string> {
  const response = await apiRequest<IdResponse>(`/employees/${employeeId}`, {
    method: 'PUT',
    body: {
      firstName: input.firstName,
      lastName: input.lastName,
      email: input.email,
      phone: input.phone,
      dateOfBirth: input.dateOfBirth,
      hireDate: input.hireDate,
      status: input.status,
    },
  })

  return response.id
}

export async function deleteEmployee(employeeId: string): Promise<void> {
  await apiRequest<void>(`/employees/${employeeId}`, {
    method: 'DELETE',
  })
}
