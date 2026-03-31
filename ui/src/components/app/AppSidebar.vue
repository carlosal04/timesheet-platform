<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { useSessionStore } from '@/stores/session'

const route = useRoute()
const session = useSessionStore()

const navItems = computed(() => {
  if (session.roleCode === 'Admin') {
    return [
      { label: 'Employees', to: '/employees' },
      { label: 'Users & Roles', to: '/roles' },
      { label: 'Audit Logs', to: '/audit-logs' },
    ]
  }

  if (session.roleCode === 'HR') {
    return [{ label: 'Employees', to: '/employees' }]
  }

  if (session.roleCode === 'Manager') {
    return [
      { label: 'Employees', to: '/employees' },
      { label: 'My Addresses', to: '/me/addresses' },
    ]
  }

  return [
    { label: 'My Addresses', to: '/me/addresses' },
  ]
})
</script>

<template>
  <div class="sidebar">
    <div class="sidebar__brand">EMS</div>
    <div class="sidebar__role status-pill status-pill--brand">{{ session.roleCode ?? 'Authenticated' }} workspace</div>

    <nav class="sidebar__nav">
      <RouterLink
        v-for="item in navItems"
        :key="item.to"
        :to="item.to"
        class="sidebar__link"
        :class="{ 'sidebar__link--active': route.path.startsWith(item.to) }"
      >
        {{ item.label }}
      </RouterLink>
    </nav>

    <div class="sidebar__meta">
      <div class="sidebar__panel">
        <span class="text-muted">Current user</span>
        <strong>{{ session.currentUser?.name ?? 'Unassigned' }}</strong>
      </div>
      <div class="sidebar__panel">
        <span class="text-muted">Expires in</span>
        <strong>{{ session.remainingLabel }}</strong>
      </div>
    </div>
  </div>
</template>

<style scoped>
.sidebar {
  display: grid;
  gap: 1rem;
  height: 100%;
  padding: 1.25rem;
  color: var(--surface-dark-text);
  border-radius: 22px;
  background: linear-gradient(180deg, var(--surface-dark), var(--surface-dark-muted));
}

.sidebar__brand {
  font-size: 1.9rem;
  font-weight: 700;
  letter-spacing: -0.04em;
}

.sidebar__role {
  width: fit-content;
}

.sidebar__nav,
.sidebar__meta {
  display: grid;
  gap: 0.625rem;
}

.sidebar__link {
  display: inline-flex;
  align-items: center;
  min-height: 46px;
  padding: 0 0.9rem;
  border-radius: 14px;
  color: rgba(226, 232, 240, 0.78);
  transition: background-color 160ms ease;
}

.sidebar__link--active {
  color: white;
  background: rgba(96, 165, 250, 0.18);
}

.sidebar__panel {
  display: grid;
  gap: 0.2rem;
  padding: 0.9rem;
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.06);
}
</style>
