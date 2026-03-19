import type { Pinia } from 'pinia'
import type { Router } from 'vue-router'
import { useSessionStore } from '@/stores/session'

export function installRouterGuards(router: Router, pinia: Pinia) {
  router.beforeEach(async (to) => {
    const session = useSessionStore(pinia)
    await session.ensureInitialized()

    if (to.meta.publicOnly && session.isAuthenticated) {
      return { name: 'employees' }
    }

    if (to.meta.requiresAuth && !session.isAuthenticated) {
      return { name: 'login', query: { redirect: to.fullPath } }
    }

    if (to.meta.roles && session.isAuthenticated && !to.meta.roles.includes(session.roleCode)) {
      return { name: 'forbidden' }
    }

    return true
  })
}
