<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useSessionStore } from '@/stores/session'
import { useThemeStore } from '@/stores/theme'

const router = useRouter()
const session = useSessionStore()
const theme = useThemeStore()

async function handleLogout() {
  await session.logout()
  await router.push({ name: 'login' })
}
</script>

<template>
  <header class="topbar section-card">
    <div>
      <p class="topbar__eyebrow">Employment Management System</p>
      <h1 class="topbar__title">Operations workspace</h1>
    </div>

    <div class="topbar__actions">
      <UBadge color="neutral" variant="soft">
        {{ theme.isDark ? 'Dark theme' : 'Light theme' }}
      </UBadge>
      <UBadge color="primary" variant="soft">
        {{ session.currentUser?.email ?? 'Signed out' }}
      </UBadge>
      <UButton color="neutral" variant="soft" @click="theme.toggleTheme()">
        Switch to {{ theme.isDark ? 'light' : 'dark' }}
      </UButton>
      <UButton color="primary" variant="soft" @click="handleLogout">Log out</UButton>
    </div>
  </header>
</template>

<style scoped>
.topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.2rem 1.3rem;
}

.topbar__eyebrow {
  margin: 0;
  font-size: 0.78rem;
  font-weight: 700;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.topbar__title {
  margin: 0.3rem 0 0;
  font-size: 1.7rem;
  letter-spacing: -0.04em;
}

.topbar__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  justify-content: flex-end;
}
</style>
