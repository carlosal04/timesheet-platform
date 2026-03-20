<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import EmptyState from '@/components/shared/EmptyState.vue'
import PageHeader from '@/components/app/PageHeader.vue'
import ProblemStatePanel from '@/components/shared/ProblemStatePanel.vue'
import { listRoles } from '@/services/api/roles'
import { assignUserRole, listUsers } from '@/services/api/users'
import { isApiProblemError, isProblemStatus } from '@/services/api/http'
import { useSessionStore } from '@/stores/session'
import type { RoleRecord, UserListRow } from '@/types/ems'

const route = useRoute()
const router = useRouter()
const session = useSessionStore()
const includeInactive = ref(false)
const emailFilter = ref('')
const roleFilter = ref('All')
const page = ref(1)
const pageSize = ref(10)
const roles = ref<RoleRecord[]>([])
const users = ref<UserListRow[]>([])
const totalCount = ref(0)
const loading = ref(true)
const problem = ref<null | { code: string; title: string; description: string }>(null)
const assignmentFeedback = ref<null | { kind: 'success' | 'error'; title: string; description: string }>(null)
const selectedRoleIds = ref<Record<string, string>>({})
const busyUserId = ref<string | null>(null)
let requestSequence = 0

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))
const activeRoleOptions = computed(() => roles.value.filter((role) => role.isActive))

function syncRoleSelections(rows: UserListRow[]) {
  const nextSelections: Record<string, string> = {}

  for (const row of rows) {
    const currentSelection = selectedRoleIds.value[row.id]
    const currentRoleStillAssignable = activeRoleOptions.value.some((role) => role.id === row.roleId)
    nextSelections[row.id] = currentSelection ?? (currentRoleStillAssignable ? row.roleId : '')
  }

  selectedRoleIds.value = nextSelections
}

function hasPendingRoleChange(user: UserListRow) {
  const selectedRoleId = selectedRoleIds.value[user.id] ?? ''
  return selectedRoleId.length > 0 && selectedRoleId !== user.roleId
}

async function applyRoleChange(user: UserListRow) {
  const selectedRoleId = selectedRoleIds.value[user.id] ?? ''
  if (!selectedRoleId || selectedRoleId === user.roleId) {
    return
  }

  busyUserId.value = user.id
  assignmentFeedback.value = null

  try {
    const result = await assignUserRole(user.id, selectedRoleId)
    await loadPage()

    assignmentFeedback.value = {
      kind: 'success',
      title: 'Role updated',
      description: `${user.email} is now assigned to ${result.roleCode}. ${result.sessionsRevoked} active session(s) were revoked.`,
    }
  } catch (error) {
    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      await router.replace({ name: 'login', query: { redirect: route.fullPath } })
      return
    }

    if (isProblemStatus(error, 404)) {
      await loadPage()
      assignmentFeedback.value = {
        kind: 'error',
        title: 'Assignment target changed',
        description: 'The selected user or role no longer exists. The user list was refreshed.',
      }
      return
    }

    if (isApiProblemError(error)) {
      assignmentFeedback.value = {
        kind: 'error',
        title: error.problem.title ?? 'Role update failed',
        description: error.problem.detail ?? 'The EMS API rejected the role assignment request.',
      }
      return
    }

    assignmentFeedback.value = {
      kind: 'error',
      title: 'Role update failed',
      description: 'The EMS API rejected the role assignment request.',
    }
  } finally {
    if (busyUserId.value === user.id) {
      busyUserId.value = null
    }
  }
}

async function loadPage() {
  const sequence = ++requestSequence
  loading.value = true
  problem.value = null

  try {
    const [roleResult, userResult] = await Promise.all([
      listRoles(includeInactive.value),
      listUsers({
        page: page.value,
        pageSize: pageSize.value,
        email: emailFilter.value.trim() || undefined,
        roleCode: roleFilter.value === 'All' ? undefined : roleFilter.value,
        includeInactive: includeInactive.value,
      }),
    ])

    if (sequence !== requestSequence) {
      return
    }

    roles.value = roleResult
    users.value = userResult.items
    totalCount.value = userResult.totalCount
    page.value = userResult.page
    pageSize.value = userResult.pageSize
    syncRoleSelections(userResult.items)
  } catch (error) {
    if (sequence !== requestSequence) {
      return
    }

    users.value = []
    totalCount.value = 0

    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      await router.replace({ name: 'login', query: { redirect: route.fullPath } })
      return
    }

    if (isApiProblemError(error)) {
      problem.value = {
        code: String(error.status || 500),
        title: error.problem.title ?? 'Role assignment data unavailable',
        description: error.problem.detail ?? 'The EMS role catalog or user list could not be loaded.',
      }
      return
    }

    problem.value = {
      code: '500',
      title: 'Role assignment data unavailable',
      description: 'The EMS role catalog or user list could not be loaded.',
    }
  } finally {
    if (sequence === requestSequence) {
      loading.value = false
    }
  }
}

function resetFilters() {
  emailFilter.value = ''
  roleFilter.value = 'All'
  includeInactive.value = false
  page.value = 1
  assignmentFeedback.value = null
}

watch([emailFilter, roleFilter, includeInactive], () => {
  page.value = 1
})

watch([emailFilter, roleFilter, includeInactive, page], () => {
  void loadPage()
})

watch(
  () => route.fullPath,
  () => {
    void loadPage()
  },
  { immediate: true },
)
</script>

<template>
  <section class="view-stack">
    <PageHeader
      title="Roles"
      description="Admin-only role catalog and user-role assignment workspace sourced from the real EMS backend."
    >
      <template #actions>
        <label class="toggle text-muted">
          <input v-model="includeInactive" type="checkbox" />
          Include inactive
        </label>
      </template>
    </PageHeader>

    <div v-if="loading" class="section-card">
      <p class="text-muted">Loading role catalog and assignable users...</p>
    </div>

    <ProblemStatePanel
      v-else-if="problem"
      :code="problem.code"
      :title="problem.title"
      :description="problem.description"
    />

    <template v-else>
      <div class="section-card roles-card">
        <div class="roles-card__header">
          <div>
            <h2>Canonical roles</h2>
            <p class="text-muted">Available roles from the EMS role catalog.</p>
          </div>
          <span class="status-pill status-pill--brand">{{ roles.length }} roles</span>
        </div>

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

      <div class="section-card filters">
        <input v-model="emailFilter" class="filters__input" placeholder="Filter by email" />
        <select v-model="roleFilter" class="filters__input">
          <option value="All">All roles</option>
          <option v-for="role in roles" :key="role.id" :value="role.code">{{ role.code }}</option>
        </select>
        <button class="filters__button" type="button" @click="resetFilters">Reset</button>
      </div>

      <div
        v-if="assignmentFeedback && assignmentFeedback.kind === 'success'"
        class="assignment-feedback assignment-feedback--success"
      >
        <strong>{{ assignmentFeedback.title }}</strong>
        <p>{{ assignmentFeedback.description }}</p>
      </div>

      <ProblemStatePanel
        v-else-if="assignmentFeedback && assignmentFeedback.kind === 'error'"
        code="Role"
        :title="assignmentFeedback.title"
        :description="assignmentFeedback.description"
      />

      <EmptyState
        v-if="!users.length"
        title="No users match the current filters."
        description="Adjust the email or role filter to widen the assignment list."
      />

      <div v-else class="section-card roles-card">
        <div class="roles-card__header">
          <div>
            <h2>Assign roles</h2>
            <p class="text-muted">Select a user, review the current role, and prepare a replacement role.</p>
          </div>
          <span class="text-muted">Page {{ page }} of {{ totalPages }}</span>
        </div>

        <table class="table">
          <thead>
            <tr>
              <th>User</th>
              <th>Linked employee</th>
              <th>Status</th>
              <th>Current role</th>
              <th>Next role</th>
              <th>Action</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="user in users" :key="user.id">
              <td>
                <strong>{{ user.email }}</strong>
              </td>
              <td>{{ user.employeeName ?? 'Not linked' }}</td>
              <td>
                <span
                  class="status-pill"
                  :class="user.isActive ? 'status-pill--success' : 'status-pill--warning'"
                >
                  {{ user.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td>
                <div>{{ user.roleName }}</div>
                <div class="text-muted">{{ user.roleCode }}</div>
              </td>
              <td>
                <select v-model="selectedRoleIds[user.id]" class="filters__input filters__input--compact">
                  <option value="">Select active role</option>
                  <option v-for="role in activeRoleOptions" :key="role.id" :value="role.id">
                    {{ role.name }} ({{ role.code }})
                  </option>
                </select>
              </td>
              <td>
                <UButton
                  color="neutral"
                  variant="soft"
                  :loading="busyUserId === user.id"
                  :disabled="busyUserId !== null || !hasPendingRoleChange(user)"
                  @click="void applyRoleChange(user)"
                >
                  Apply
                </UButton>
              </td>
            </tr>
          </tbody>
        </table>

        <div class="table-card__footer">
          <span class="text-muted">{{ totalCount }} matching users</span>
          <div class="table-card__pagination">
            <UButton color="neutral" variant="soft" :disabled="page === 1" @click="page -= 1">
              Previous
            </UButton>
            <UButton color="neutral" variant="soft" :disabled="page === totalPages" @click="page += 1">
              Next
            </UButton>
          </div>
        </div>
      </div>
    </template>
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

.roles-card__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1rem;
}

.roles-card__header h2 {
  margin: 0;
  font-size: 1rem;
}

.roles-card__header p {
  margin: 0.35rem 0 0;
}

.filters {
  display: grid;
  grid-template-columns: minmax(0, 1.5fr) 220px auto;
  gap: 0.75rem;
  padding: 1rem;
}

.filters__input {
  min-height: 44px;
  padding: 0 0.85rem;
  border: 1px solid var(--panel-border);
  border-radius: 14px;
  background: var(--panel-bg-strong);
  color: var(--text-main);
}

.filters__input--compact {
  min-width: 220px;
}

.filters__button {
  min-height: 44px;
  padding: 0 1rem;
  border: 1px solid var(--panel-border);
  border-radius: 14px;
  background: transparent;
  color: var(--text-main);
}

.table {
  width: 100%;
  border-collapse: collapse;
}

.table th {
  text-align: left;
  padding: 0.75rem 0;
  font-size: 0.76rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  border-bottom: 1px solid var(--panel-border);
}

.table td {
  padding: 1rem 0;
  border-bottom: 1px solid var(--panel-border);
  vertical-align: top;
}

.toggle {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
}

.table-card__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding-top: 1rem;
}

.table-card__pagination {
  display: flex;
  gap: 0.75rem;
}

.assignment-feedback {
  padding: 1rem 1.1rem;
  border-radius: 16px;
  border: 1px solid var(--panel-border);
}

.assignment-feedback--success {
  border-color: color-mix(in srgb, var(--success) 32%, var(--panel-border));
  background: color-mix(in srgb, var(--success-soft) 82%, var(--panel-bg));
  color: var(--success);
}

.assignment-feedback p {
  margin: 0.4rem 0 0;
}

@media (max-width: 1100px) {
  .filters {
    grid-template-columns: 1fr;
  }

  .roles-card__header,
  .table-card__footer {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
