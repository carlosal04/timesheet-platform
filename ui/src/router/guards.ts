import type { Pinia } from 'pinia'
import type { Router } from 'vue-router'
import { useSessionStore } from '@/stores/session'

export function installRouterGuards(router: Router, pinia: Pinia) {
  router.beforeEach(async (to) => {
    const session = useSessionStore(pinia)
    await session.ensureInitialized()

    if (to.name === 'root') {
      if (!session.isAuthenticated) {
        return { name: 'login' }
      }

      return session.defaultAuthenticatedPath
    }

    if (session.isAuthenticated && session.mustChangePassword && to.name !== 'change-password') {
      return { name: 'change-password' }
    }

    if (to.meta.publicOnly && session.isAuthenticated) {
      return session.defaultAuthenticatedPath
    }

    if (to.meta.requiresAuth && !session.isAuthenticated) {
      return { name: 'login', query: { redirect: to.fullPath } }
    }

    if (to.meta.roles && session.roleCode && session.isAuthenticated && !to.meta.roles.includes(session.roleCode)) {
      return { name: 'forbidden' }
    }

    return true
  })
}
