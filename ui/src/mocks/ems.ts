import type { AuditRecord, EmployeeAddress, EmployeeListRow, EmployeeRecord, RoleRecord, RoleCode, SessionSnapshot, UserRecord } from '@/types/ems'

function address(
  id: string,
  addressType: EmployeeAddress['addressType'],
  isPrimary: boolean,
  line1: string,
  city: string,
  state: string,
  zipCode: string,
  createdAtUtc: string,
  line2: string | null = null,
): EmployeeAddress {
  return {
    id,
    addressType,
    isPrimary,
    line1,
    line2,
    city,
    state,
    zipCode,
    countryCode: 'US',
    createdAtUtc,
    deletedAtUtc: null,
  }
}

export const mockEmployees: EmployeeRecord[] = [
  {
    id: 'emp-ana-lopez',
    firstName: 'Ana',
    lastName: 'Lopez',
    email: 'ana.lopez@company.com',
    phone: '5551234567',
    dateOfBirth: '1990-06-18',
    hireDate: '2025-01-15',
    status: 'Active',
    addresses: [
      address('addr-ana-home', 'Home', true, '100 Main St', 'Pittsburgh', 'PA', '15222', '2026-01-10T10:00:00Z'),
      address('addr-ana-mail', 'Mailing', false, '200 Oak Ave', 'Pittsburgh', 'PA', '15213', '2026-01-15T12:00:00Z', 'Apt 4'),
    ],
  },
  {
    id: 'emp-marcus-hill',
    firstName: 'Marcus',
    lastName: 'Hill',
    email: 'marcus.hill@company.com',
    phone: '5552345678',
    dateOfBirth: '1988-09-04',
    hireDate: '2024-04-10',
    status: 'Inactive',
    addresses: [
      address('addr-marcus-home', 'Home', true, '44 Lake Dr', 'Chicago', 'IL', '60611', '2026-02-11T08:30:00Z'),
    ],
  },
  {
    id: 'emp-jasmine-cho',
    firstName: 'Jasmine',
    lastName: 'Cho',
    email: 'jasmine.cho@company.com',
    phone: '5553456789',
    dateOfBirth: '1994-11-12',
    hireDate: '2023-06-02',
    status: 'Active',
    addresses: [
      address('addr-jasmine-home', 'Home', true, '76 Park Row', 'Seattle', 'WA', '98101', '2026-01-03T09:00:00Z'),
    ],
  },
  {
    id: 'emp-david-price',
    firstName: 'David',
    lastName: 'Price',
    email: 'david.price@company.com',
    phone: '5554567890',
    dateOfBirth: '1987-02-21',
    hireDate: '2022-09-12',
    status: 'Active',
    addresses: [
      address('addr-david-home', 'Home', true, '18 River St', 'Albany', 'NY', '12207', '2026-01-21T14:20:00Z'),
      address('addr-david-mail', 'Mailing', false, '44 Pine Ave', 'Albany', 'NY', '12203', '2026-01-24T11:10:00Z'),
    ],
  },
  {
    id: 'emp-sophia-kim',
    firstName: 'Sophia',
    lastName: 'Kim',
    email: 'sophia.kim@company.com',
    phone: '5555678901',
    dateOfBirth: '1992-03-08',
    hireDate: '2023-02-18',
    status: 'Active',
    addresses: [
      address('addr-sophia-home', 'Home', true, '90 West Ave', 'Austin', 'TX', '73301', '2026-01-08T13:40:00Z'),
    ],
  },
  {
    id: 'emp-evelyn-brooks',
    firstName: 'Evelyn',
    lastName: 'Brooks',
    email: 'evelyn.brooks@company.com',
    phone: '5556789012',
    dateOfBirth: '1991-07-29',
    hireDate: '2021-10-06',
    status: 'Active',
    addresses: [
      address('addr-evelyn-home', 'Home', true, '3 Union Sq', 'Boston', 'MA', '02108', '2026-01-19T15:30:00Z'),
    ],
  },
]

export const mockUsers: UserRecord[] = [
  {
    id: 'user-admin-carlos',
    name: 'Carlos Alvarez',
    email: 'admin@company.com',
    roleCode: 'Admin',
    employeeId: null,
  },
  {
    id: 'user-basic-david',
    name: 'David Price',
    email: 'basic@company.com',
    roleCode: 'Basic',
    employeeId: 'emp-david-price',
  },
  {
    id: 'user-admin-ops',
    name: 'Operations Lead',
    email: 'ops.lead@company.com',
    roleCode: 'Admin',
    employeeId: null,
  },
]

export const mockRoles: RoleRecord[] = [
  { id: 'role-admin', code: 'Admin', name: 'Administrator', isActive: true },
  { id: 'role-basic', code: 'Basic', name: 'Basic User', isActive: true },
]

export const mockAuditRecords: AuditRecord[] = [
  {
    id: 'audit-1',
    occurredAtUtc: '2026-03-19T09:31:00Z',
    actionType: 'SessionRenewed',
    actorEmail: 'admin@company.com',
    entityType: 'UserSession',
    summary: 'Session renewed from the shell warning banner.',
  },
  {
    id: 'audit-2',
    occurredAtUtc: '2026-03-19T09:22:00Z',
    actionType: 'RoleChanged',
    actorEmail: 'admin@company.com',
    entityType: 'User',
    summary: 'Marcus Hill moved from Basic to Admin.',
  },
  {
    id: 'audit-3',
    occurredAtUtc: '2026-03-18T18:08:00Z',
    actionType: 'EmployeeCreated',
    actorEmail: 'ops.lead@company.com',
    entityType: 'Employee',
    summary: 'Created Sophia Kim with one initial address.',
  },
]

export function getPrimaryAddress(employee: EmployeeRecord) {
  return employee.addresses.find((addressRecord) => addressRecord.isPrimary) ?? null
}

export function toEmployeeListRows(): EmployeeListRow[] {
  return mockEmployees.map((employee) => ({
    id: employee.id,
    firstName: employee.firstName,
    lastName: employee.lastName,
    email: employee.email,
    phone: employee.phone,
    dateOfBirth: employee.dateOfBirth,
    hireDate: employee.hireDate,
    status: employee.status,
    primaryAddress: getPrimaryAddress(employee),
  }))
}

export function findEmployee(employeeId: string) {
  return mockEmployees.find((employee) => employee.id === employeeId) ?? null
}

export function buildMockSession(roleCode: RoleCode): SessionSnapshot {
  const user = mockUsers.find((candidate) => candidate.roleCode === roleCode)!

  return {
    userId: user.id,
    email: user.email,
    roleCode: user.roleCode,
    employeeId: user.employeeId,
    sessionId: `session-${roleCode.toLowerCase()}`,
    expiresAtUtc: new Date(Date.now() + 27 * 60 * 1000).toISOString(),
    idleTimeoutMinutes: 480,
  }
}
