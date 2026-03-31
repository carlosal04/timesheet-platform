import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { useNow } from '@vueuse/core'
import { findEmployee, mockAuditRecords, mockRoles, mockUsers, toEmployeeListRows } from '@/mocks/ems'
import {
  getSession,
  login as loginRequest,
  logout as logoutRequest,
  renewSession as renewSessionRequest,
} from '@/services/api/auth'
import { clearAntiforgeryToken, isApiProblemError } from '@/services/api/http'
import type { AuditRecord, EmployeeListRow, EmployeeRecord, LoginProblem, RoleCode, RoleRecord, SessionSnapshot, UserRecord } from '@/types/ems'
import { formatRemainingTime } from '@/utils/date'

const IDLE_WARNING_MINUTES = 30

export const useSessionStore = defineStore('session', () => {
  const now = useNow({ interval: 1000 })
  const session = ref<SessionSnapshot | null>(null)
  const loginProblem = ref<LoginProblem | null>(null)
  const isBusy = ref(false)
  const isInitialized = ref(false)
  const isHydrating = ref(false)
  let initializationPromise: Promise<void> | null = null

  const isAuthenticated = computed(() => session.value !== null)
  const roleCode = computed<RoleCode | null>(() => session.value?.roleCode ?? null)
  const mustChangePassword = computed(() => session.value?.mustChangePassword ?? false)
  const remainingMilliseconds = computed(() => {
    if (!session.value) {
      return 0
    }

    return Math.max(0, new Date(session.value.expiresAtUtc).getTime() - now.value.getTime())
  })
  const remainingLabel = computed(() => formatRemainingTime(remainingMilliseconds.value))
  const defaultAuthenticatedPath = computed(() => {
    if (mustChangePassword.value) {
      return '/change-password'
    }

    return roleCode.value === 'Developer' ? '/me/addresses' : '/employees'
  })
  const isInWarningWindow = computed(
    () =>
      isAuthenticated.value &&
      remainingMilliseconds.value > 0 &&
      remainingMilliseconds.value <= IDLE_WARNING_MINUTES * 60 * 1000,
  )
  const currentUser = computed<UserRecord | null>(() => {
    if (!session.value) {
      return null
    }

    const matchingMockUser = mockUsers.find((user) => user.email === session.value?.email)
    if (matchingMockUser) {
      return matchingMockUser
    }

    const emailName = session.value.email.split('@')[0] ?? 'Authenticated user'
    const displayName = emailName
      .split(/[.\-_]/g)
      .filter(Boolean)
      .map((segment) => segment.charAt(0).toUpperCase() + segment.slice(1))
      .join(' ')

    return {
      id: session.value.userId,
      name: displayName || 'Authenticated user',
      email: session.value.email,
      roleCode: session.value.roleCode,
      employeeId: session.value.employeeId,
    }
  })
  const employeeRows = computed<EmployeeListRow[]>(() => toEmployeeListRows())
  const currentEmployee = computed<EmployeeRecord | null>(() => {
    if (!session.value?.employeeId) {
      return null
    }

    return findEmployee(session.value.employeeId)
  })

  function buildProblem(status: number, title: string, detail: string): LoginProblem {
    return {
      status,
      title,
      detail,
    }
  }

  function toLoginProblem(error: unknown): LoginProblem {
    if (isApiProblemError(error)) {
      switch (error.status) {
        case 401:
          return buildProblem(
            401,
            error.problem.title ?? 'Invalid credentials',
            error.problem.detail ?? 'The email or password you entered is not valid.',
          )
        case 423:
          return buildProblem(
            423,
            error.problem.title ?? 'Account is temporarily locked',
            error.problem.detail ?? 'This account is temporarily locked after repeated failed attempts.',
          )
        case 429:
          return buildProblem(
            429,
            error.problem.title ?? 'Too many login attempts',
            error.problem.detail ?? 'Please wait a moment before trying again.',
          )
        default:
          return buildProblem(
            error.status,
            error.problem.title ?? 'Unable to sign in',
            error.problem.detail ?? 'The EMS API rejected this request.',
          )
      }
    }

    return buildProblem(0, 'Unable to reach EMS API', 'The API is unavailable or the session bootstrap failed.')
  }

  async function ensureInitialized() {
    if (isInitialized.value) {
      return
    }

    if (initializationPromise) {
      return initializationPromise
    }

    isHydrating.value = true
    initializationPromise = (async () => {
      try {
        session.value = await getSession()
      } catch (error) {
        session.value = null
        loginProblem.value = toLoginProblem(error)
      } finally {
        isInitialized.value = true
        isHydrating.value = false
        initializationPromise = null
      }
    })()

    return initializationPromise
  }

  async function login(email: string, password: string) {
    loginProblem.value = null
    isBusy.value = true
    isInitialized.value = true

    try {
      session.value = await loginRequest(email, password)
      return true
    } catch (error) {
      session.value = null
      loginProblem.value = toLoginProblem(error)
      return false
    } finally {
      isBusy.value = false
    }
  }

  async function renewSession() {
    if (!session.value) {
      return false
    }

    isBusy.value = true

    try {
      const renewed = await renewSessionRequest()
      if (!renewed) {
        session.value = null
        return false
      }

      session.value = {
        ...session.value,
        sessionId: renewed.sessionId,
        expiresAtUtc: renewed.expiresAtUtc,
        idleTimeoutMinutes: renewed.idleTimeoutMinutes,
      }
      return true
    } catch (error) {
      loginProblem.value = toLoginProblem(error)
      return false
    } finally {
      isBusy.value = false
    }
  }

  async function refreshSession() {
    try {
      session.value = await getSession()
      return session.value !== null
    } catch (error) {
      loginProblem.value = toLoginProblem(error)
      return false
    }
  }

  async function logout() {
    isBusy.value = true

    try {
      await logoutRequest()
    } finally {
      session.value = null
      clearAntiforgeryToken()
      isBusy.value = false
    }
  }

  function handleUnauthorized() {
    session.value = null
    clearAntiforgeryToken()
    loginProblem.value = buildProblem(
      401,
      'Authentication required',
      'Your session ended or is no longer valid. Sign in again to continue.',
    )
  }

  function clearProblem() {
    loginProblem.value = null
  }

  return {
    auditRecords: mockAuditRecords as AuditRecord[],
    currentEmployee,
    currentUser,
    employeeRows,
    isAuthenticated,
    isBusy,
    isHydrating,
    isInitialized,
    isInWarningWindow,
    loginProblem,
    mustChangePassword,
    mockRoles: mockRoles as RoleRecord[],
    mockUsers: mockUsers as UserRecord[],
    defaultAuthenticatedPath,
    remainingLabel,
    remainingMilliseconds,
    roleCode,
    session,
    clearProblem,
    ensureInitialized,
    handleUnauthorized,
    login,
    logout,
    refreshSession,
    renewSession,
  }
})
