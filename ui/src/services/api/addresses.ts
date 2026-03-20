import type { EmployeeAddress } from '@/types/ems'
import { apiRequest } from '@/services/api/http'

interface EmployeeAddressListResponse {
  employeeId: string
  items: EmployeeAddressItem[]
}

interface EmployeeAddressItem {
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

function toEmployeeAddress(item: EmployeeAddressItem): EmployeeAddress {
  return {
    id: item.id,
    addressType: item.addressType,
    isPrimary: item.isPrimary,
    line1: item.line1,
    line2: item.line2,
    city: item.city,
    state: item.state,
    zipCode: item.zipCode,
    countryCode: item.countryCode,
  }
}

export async function listEmployeeAddresses(employeeId: string, includeDeleted = false) {
  const query = includeDeleted ? '?includeDeleted=true' : ''
  const response = await apiRequest<EmployeeAddressListResponse>(`/employees/${employeeId}/addresses${query}`)
  return response.items.map(toEmployeeAddress)
}

export async function setEmployeeAddressPrimary(employeeId: string, addressId: string) {
  await apiRequest<void>(`/employees/${employeeId}/addresses/${addressId}/primary`, {
    method: 'PATCH',
  })
}

export async function deleteEmployeeAddress(employeeId: string, addressId: string) {
  await apiRequest<void>(`/employees/${employeeId}/addresses/${addressId}`, {
    method: 'DELETE',
  })
}

export async function listOwnAddresses() {
  const response = await apiRequest<EmployeeAddressListResponse>('/me/addresses')
  return response.items.map(toEmployeeAddress)
}

export async function setOwnAddressPrimary(addressId: string) {
  await apiRequest<void>(`/me/addresses/${addressId}/primary`, {
    method: 'PATCH',
  })
}

export async function deleteOwnAddress(addressId: string) {
  await apiRequest<void>(`/me/addresses/${addressId}`, {
    method: 'DELETE',
  })
}
