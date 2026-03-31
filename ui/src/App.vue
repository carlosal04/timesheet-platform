<script setup lang="ts">
import { computed } from 'vue'
import { RouterView, useRoute } from 'vue-router'
import AppShell from '@/components/app/AppShell.vue'
import { useSessionStore } from '@/stores/session'

const route = useRoute()
const session = useSessionStore()
const shelllessRouteNames = new Set(['login', 'forgot-password', 'reset-password', 'change-password'])
const showShell = computed(() => session.isAuthenticated && !shelllessRouteNames.has(String(route.name ?? '')))
</script>

<template>
  <UApp>
    <RouterView v-slot="{ Component }">
      <AppShell v-if="showShell && Component">
        <component :is="Component" />
      </AppShell>
      <component :is="Component" v-else-if="Component" />
    </RouterView>
  </UApp>
</template>
