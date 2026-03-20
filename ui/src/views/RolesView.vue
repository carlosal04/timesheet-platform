<script setup lang="ts">
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import PageHeader from '@/components/app/PageHeader.vue'
import ProblemStatePanel from '@/components/shared/ProblemStatePanel.vue'
import { listRoles } from '@/services/api/roles'
import { isApiProblemError, isProblemStatus } from '@/services/api/http'
import { useSessionStore } from '@/stores/session'
import type { RoleRecord } from '@/types/ems'

const route = useRoute()
const router = useRouter()
const session = useSessionStore()
const includeInactive = ref(false)
const roles = ref<RoleRecord[]>([])
const loading = ref(true)
const problem = ref<null | { code: string; title: string; description: string }>(null)

async function loadRoles() {
  loading.value = true
  problem.value = null

  try {
    roles.value = await listRoles(includeInactive.value)
  } catch (error) {
    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      await router.replace({ name: 'login', query: { redirect: route.fullPath } })
      return
    }

    if (isApiProblemError(error)) {
      problem.value = {
        code: String(error.status || 500),
        title: error.problem.title ?? 'Role catalog unavailable',
        description: error.problem.detail ?? 'The canonical role catalog could not be loaded from the EMS API.',
      }
      return
    }

    problem.value = {
      code: '500',
      title: 'Role catalog unavailable',
      description: 'The canonical role catalog could not be loaded from the EMS API.',
    }
  } finally {
    loading.value = false
  }
}

watch(includeInactive, () => {
  void loadRoles()
})

watch(
  () => route.fullPath,
  () => {
    void loadRoles()
  },
  { immediate: true },
)
</script>

<template>
  <section class="view-stack">
    <PageHeader
      title="Roles"
      description="Admin-only role catalog sourced from the real EMS backend."
    >
      <template #actions>
        <label class="toggle text-muted">
          <input v-model="includeInactive" type="checkbox" />
          Include inactive
        </label>
      </template>
    </PageHeader>

    <ProblemStatePanel
      code="Info"
      title="Role assignment UI is waiting on a user-read contract."
      description="The approved backend exposes GET /roles and PATCH /users/{userId}/role, but it does not yet expose a user-list/read endpoint for selecting the target user in a trustworthy UI."
    />

    <div v-if="loading" class="section-card">
      <p class="text-muted">Loading canonical roles...</p>
    </div>

    <ProblemStatePanel
      v-else-if="problem"
      :code="problem.code"
      :title="problem.title"
      :description="problem.description"
    />

    <div v-else class="section-card roles-card">
      <table class="table">
        <thead>
          <tr>
            <th>Code</th>
            <th>Name</th>
            <th>Status</th>
            <th>System role</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="role in roles" :key="role.id">
            <td><span class="status-pill status-pill--primary">{{ role.code }}</span></td>
            <td>{{ role.name }}</td>
            <td>
              <span
                class="status-pill"
                :class="role.isActive ? 'status-pill--success' : 'status-pill--warning'"
              >
                {{ role.isActive ? 'Active' : 'Inactive' }}
              </span>
            </td>
            <td>{{ role.isSystem ? 'Yes' : 'No' }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>

<style scoped>
.view-stack {
  display: grid;
  gap: 1rem;
}

.roles-card {
  padding: 1rem;
}

.table {
  width: 100%;
  border-collapse: collapse;
}

.table th,
.table td {
  text-align: left;
  padding: 0.9rem 0;
  border-bottom: 1px solid var(--panel-border);
}

.toggle {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
}
</style>
