import type { RoleCode, SessionSnapshot } from '@/types/ems'
import { apiRequest, clearAntiforgeryToken, ensureAntiforgeryToken, isProblemStatus } from '@/services/api/http'

interface LoginResponse {
  userId: string
  email: string
  roleCode: RoleCode
}

interface SessionResponse {
  userId: string
  email: string
  roleCode: RoleCode
  employeeId: string | null
  sessionId: string
  expiresAtUtc: string
  idleTimeoutMinutes: number
}

interface RenewSessionResponse {
  sessionId: string
  expiresAtUtc: string
  idleTimeoutMinutes: number
}

function normalizeSession(response: SessionResponse): SessionSnapshot {
  return {
    userId: response.userId,
    email: response.email,
    roleCode: response.roleCode,
    employeeId: response.employeeId,
    sessionId: response.sessionId,
    expiresAtUtc: response.expiresAtUtc,
    idleTimeoutMinutes: response.idleTimeoutMinutes,
  }
}

export async function login(email: string, password: string): Promise<SessionSnapshot> {
  await apiRequest<LoginResponse>('/auth/login', {
    method: 'POST',
    body: { email, password },
    skipAntiforgery: true,
  })

  const session = await getSession()
  if (!session) {
    throw new Error('Session bootstrap failed after login')
  }

  return session
}

export async function getSession(): Promise<SessionSnapshot | null> {
  try {
    const response = await apiRequest<SessionResponse>('/auth/session')
    const session = normalizeSession(response)
    await ensureAntiforgeryToken(true)
    return session
  } catch (error) {
    if (isProblemStatus(error, 401)) {
      clearAntiforgeryToken()
      return null
    }

    throw error
  }
}

export async function logout() {
  try {
    await apiRequest<void>('/auth/logout', {
      method: 'POST',
    })
  } finally {
    clearAntiforgeryToken()
  }
}

export async function renewSession(): Promise<RenewSessionResponse | null> {
  try {
    return await apiRequest<RenewSessionResponse>('/auth/renew', {
      method: 'POST',
    })
  } catch (error) {
    if (isProblemStatus(error, 401)) {
      clearAntiforgeryToken()
      return null
    }

    throw error
  }
}
