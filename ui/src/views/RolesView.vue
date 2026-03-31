<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import EmptyState from '@/components/shared/EmptyState.vue'
import PageHeader from '@/components/app/PageHeader.vue'
import ProblemStatePanel from '@/components/shared/ProblemStatePanel.vue'
import ValidationSummary from '@/components/shared/ValidationSummary.vue'
import { listEmployees } from '@/services/api/employees'
import { isApiProblemError, isProblemStatus } from '@/services/api/http'
import { listRoles } from '@/services/api/roles'
import { assignUserRole, createUser, listUsers, resendTemporaryPassword } from '@/services/api/users'
import { useSessionStore } from '@/stores/session'
import { formatUtcDateTime } from '@/utils/date'
import type { EmployeeListRow, RoleRecord, UserListRow } from '@/types/ems'

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
const employeeOptions = ref<EmployeeListRow[]>([])
const totalCount = ref(0)
const loading = ref(true)
const employeeLookupBusy = ref(false)
const createBusy = ref(false)
const problem = ref<null | { code: string; title: string; description: string }>(null)
const createProblem = ref<null | { code: string; title: string; description: string }>(null)
const actionFeedback = ref<null | { kind: 'success' | 'error'; title: string; description: string }>(null)
const createSubmitted = ref(false)
const createValidationMessages = ref<string[]>([])
const selectedRoleIds = ref<Record<string, string>>({})
const busyUserId = ref<string | null>(null)
const createForm = reactive({
  roleId: '',
  employeeSearch: '',
  employeeId: '',
  email: '',
})
let requestSequence = 0
let employeeLookupSequence = 0

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))
const activeRoleOptions = computed(() => roles.value.filter((role) => role.isActive))
const selectedCreateRole = computed(() => roles.value.find((role) => role.id === createForm.roleId) ?? null)
const requiresEmployeeLink = computed(() => {
  const roleCode = selectedCreateRole.value?.code
  return roleCode === 'Manager' || roleCode === 'Developer'
})
const selectedEmployee = computed(() => employeeOptions.value.find((employee) => employee.id === createForm.employeeId) ?? null)
const shouldEnterEmailManually = computed(() => !requiresEmployeeLink.value && !createForm.employeeId)

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

function canResendTemporaryPassword(user: UserListRow) {
  return user.mustChangePassword && Boolean(user.temporaryPasswordExpiresAtUtc)
}

function resetCreateForm() {
  createSubmitted.value = false
  createValidationMessages.value = []
  createProblem.value = null
  createForm.employeeSearch = ''
  createForm.employeeId = ''
  createForm.email = ''

  if (!createForm.roleId && activeRoleOptions.value.length) {
    createForm.roleId = activeRoleOptions.value[0]?.id ?? ''
  }
}

function buildCreateValidationMessages() {
  const messages: string[] = []

  if (!createForm.roleId) {
    messages.push('Select a role for the new user.')
  }

  if (requiresEmployeeLink.value && !createForm.employeeId) {
    messages.push('Manager and Developer accounts must be linked to an employee.')
  }

  if (shouldEnterEmailManually.value && !createForm.email.includes('@')) {
    messages.push('Enter a valid email address for Admin or HR accounts that are not linked to an employee.')
  }

  return messages
}

async function loadEmployeeOptions() {
  const sequence = ++employeeLookupSequence
  employeeLookupBusy.value = true

  try {
    const response = await listEmployees({
      page: 1,
      pageSize: 25,
      name: createForm.employeeSearch.trim() || undefined,
      includePrimaryAddress: false,
    })

    if (sequence !== employeeLookupSequence) {
      return
    }

    employeeOptions.value = response.items

    if (createForm.employeeId && !response.items.some((employee) => employee.id === createForm.employeeId)) {
      createForm.employeeId = ''
    }
  } catch (error) {
    if (sequence !== employeeLookupSequence) {
      return
    }

    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      await router.replace({ name: 'login', query: { redirect: route.fullPath } })
    }
  } finally {
    if (sequence === employeeLookupSequence) {
      employeeLookupBusy.value = false
    }
  }
}

async function applyRoleChange(user: UserListRow) {
  const selectedRoleId = selectedRoleIds.value[user.id] ?? ''
  if (!selectedRoleId || selectedRoleId === user.roleId) {
    return
  }

  busyUserId.value = user.id
  actionFeedback.value = null

  try {
    const result = await assignUserRole(user.id, selectedRoleId)
    await loadPage()

    actionFeedback.value = {
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
      actionFeedback.value = {
        kind: 'error',
        title: 'Assignment target changed',
        description: 'The selected user or role no longer exists. The user list was refreshed.',
      }
      return
    }

    if (isApiProblemError(error)) {
      actionFeedback.value = {
        kind: 'error',
        title: error.problem.title ?? 'Role update failed',
        description: error.problem.detail ?? 'The EMS API rejected the role assignment request.',
      }
      return
    }

    actionFeedback.value = {
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

async function createLoginUser() {
  createSubmitted.value = true
  createValidationMessages.value = buildCreateValidationMessages()
  createProblem.value = null
  actionFeedback.value = null

  if (createValidationMessages.value.length) {
    return
  }

  createBusy.value = true

  try {
    const result = await createUser({
      roleId: createForm.roleId,
      employeeId: createForm.employeeId || undefined,
      email: shouldEnterEmailManually.value ? createForm.email.trim() : undefined,
    })

    await loadPage()
    resetCreateForm()

    actionFeedback.value = {
      kind: 'success',
      title: 'User created',
      description: `${result.email} was created as ${result.roleCode}. The temporary password expires ${formatUtcDateTime(result.temporaryPasswordExpiresAtUtc)}.`,
    }
  } catch (error) {
    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      await router.replace({ name: 'login', query: { redirect: route.fullPath } })
      return
    }

    if (isApiProblemError(error)) {
      createProblem.value = {
        code: String(error.status || 400),
        title: error.problem.title ?? 'Unable to create user',
        description: error.problem.detail ?? 'The user could not be created from the current onboarding inputs.',
      }
      return
    }

    createProblem.value = {
      code: '500',
      title: 'Unable to create user',
      description: 'The user could not be created from the current onboarding inputs.',
    }
  } finally {
    createBusy.value = false
  }
}

async function resendInvite(user: UserListRow) {
  if (!canResendTemporaryPassword(user)) {
    return
  }

  busyUserId.value = user.id
  actionFeedback.value = null

  try {
    const result = await resendTemporaryPassword(user.id)
    await loadPage()

    actionFeedback.value = {
      kind: 'success',
      title: 'Temporary password reissued',
      description: `${user.email} received a new onboarding password. It expires ${formatUtcDateTime(result.temporaryPasswordExpiresAtUtc)}.`,
    }
  } catch (error) {
    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      await router.replace({ name: 'login', query: { redirect: route.fullPath } })
      return
    }

    if (isProblemStatus(error, 404)) {
      await loadPage()
      actionFeedback.value = {
        kind: 'error',
        title: 'User no longer available',
        description: 'The selected onboarding target no longer exists. The user list was refreshed.',
      }
      return
    }

    if (isApiProblemError(error)) {
      actionFeedback.value = {
        kind: 'error',
        title: error.problem.title ?? 'Unable to resend temporary password',
        description: error.problem.detail ?? 'The onboarding email could not be reissued.',
      }
      return
    }

    actionFeedback.value = {
      kind: 'error',
      title: 'Unable to resend temporary password',
      description: 'The onboarding email could not be reissued.',
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

    if (!createForm.roleId && activeRoleOptions.value.length) {
      createForm.roleId = activeRoleOptions.value[0]?.id ?? ''
    }
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
        title: error.problem.title ?? 'User management data unavailable',
        description: error.problem.detail ?? 'The EMS role catalog or user list could not be loaded.',
      }
      return
    }

    problem.value = {
      code: '500',
      title: 'User management data unavailable',
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
  actionFeedback.value = null
}

function onboardingTitle(user: UserListRow) {
  return user.mustChangePassword ? 'Pending activation' : 'Activated'
}

function onboardingDescription(user: UserListRow) {
  if (user.mustChangePassword && user.temporaryPasswordExpiresAtUtc) {
    return `Temporary password expires ${formatUtcDateTime(user.temporaryPasswordExpiresAtUtc)}`
  }

  return 'Permanent password set'
}

watch(
  () => createForm.roleId,
  () => {
    createProblem.value = null

    if (requiresEmployeeLink.value) {
      createForm.email = ''
    }
  },
)

watch(
  () => createForm.employeeId,
  () => {
    createProblem.value = null

    if (createForm.employeeId) {
      createForm.email = ''
    }
  },
)

watch([emailFilter, roleFilter, includeInactive], () => {
  page.value = 1
})

watch([emailFilter, roleFilter, includeInactive, page], () => {
  void loadPage()
})

watch(
  () => createForm.employeeSearch,
  () => {
    void loadEmployeeOptions()
  },
)

watch(
  () => route.fullPath,
  () => {
    void loadPage()
  },
  { immediate: true },
)

watch(
  activeRoleOptions,
  () => {
    if (!createForm.roleId && activeRoleOptions.value.length) {
      createForm.roleId = activeRoleOptions.value[0]?.id ?? ''
    }
  },
  { immediate: true },
)

void loadEmployeeOptions()
</script>

<template>
  <section class="view-stack">
    <PageHeader
      title="Users & Roles"
      description="Create users, monitor onboarding, reissue temporary passwords, and manage role assignments from the live EMS backend."
    >
      <template #actions>
        <label class="toggle text-muted">
          <input v-model="includeInactive" type="checkbox" />
          Include inactive
        </label>
      </template>
    </PageHeader>

    <div v-if="loading" class="section-card">
      <p class="text-muted">Loading the role catalog and user-management workspace...</p>
    </div>

    <ProblemStatePanel
      v-else-if="problem"
      :code="problem.code"
      :title="problem.title"
      :description="problem.description"
    />

    <template v-else>
      <div class="section-card create-user-card">
        <div class="create-user-card__header">
          <div>
            <h2>Create login user</h2>
            <p class="text-muted">
              Issue a new onboarding email with a 24-hour temporary password. Manager and Developer accounts
              must be linked to an employee profile.
            </p>
          </div>
          <span class="status-pill status-pill--brand">No-reply onboarding email</span>
        </div>

        <ProblemStatePanel
          v-if="createProblem"
          :code="createProblem.code"
          :title="createProblem.title"
          :description="createProblem.description"
        />

        <ValidationSummary :messages="createSubmitted ? createValidationMessages : []" />

        <div class="create-user-card__grid">
          <div class="field">
            <label for="create-role">Role</label>
            <select id="create-role" v-model="createForm.roleId" class="field__input">
              <option value="">Select role</option>
              <option v-for="role in activeRoleOptions" :key="role.id" :value="role.id">
                {{ role.name }} ({{ role.code }})
              </option>
            </select>
          </div>

          <div class="field">
            <label for="employee-search">Employee search</label>
            <input
              id="employee-search"
              v-model="createForm.employeeSearch"
              class="field__input"
              type="text"
              placeholder="Search by employee name or email"
            />
          </div>

          <div class="field">
            <label for="employee-select">Linked employee</label>
            <select id="employee-select" v-model="createForm.employeeId" class="field__input">
              <option value="">{{ employeeLookupBusy ? 'Loading employees...' : 'No employee link' }}</option>
              <option v-for="employee in employeeOptions" :key="employee.id" :value="employee.id">
                {{ employee.firstName }} {{ employee.lastName }} · {{ employee.email }}
              </option>
            </select>
          </div>

          <div v-if="shouldEnterEmailManually" class="field">
            <label for="manual-email">Email</label>
            <input
              id="manual-email"
              v-model="createForm.email"
              class="field__input"
              type="email"
              placeholder="admin.or.hr@company.com"
            />
          </div>

          <div v-else class="field field--summary">
            <label>Delivery target</label>
            <div class="field__summary">
              <strong>{{ selectedEmployee?.email ?? 'Derived from the linked employee' }}</strong>
              <span class="text-muted">
                {{ requiresEmployeeLink ? 'Required for this role' : 'Optional employee link selected' }}
              </span>
            </div>
          </div>
        </div>

        <div class="create-user-card__actions">
          <span class="text-muted">
            {{ requiresEmployeeLink ? 'This role requires an employee link.' : 'Admin and HR can be created without an employee link.' }}
          </span>
          <UButton :loading="createBusy" @click="void createLoginUser()">Create user and send invite</UButton>
        </div>
      </div>

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
        <input v-model="emailFilter" class="filters__input" placeholder="Filter users by email" />
        <select v-model="roleFilter" class="filters__input">
          <option value="All">All roles</option>
          <option v-for="role in roles" :key="role.id" :value="role.code">{{ role.code }}</option>
        </select>
        <button class="filters__button" type="button" @click="resetFilters">Reset</button>
      </div>

      <div
        v-if="actionFeedback && actionFeedback.kind === 'success'"
        class="assignment-feedback assignment-feedback--success"
      >
        <strong>{{ actionFeedback.title }}</strong>
        <p>{{ actionFeedback.description }}</p>
      </div>

      <ProblemStatePanel
        v-else-if="actionFeedback && actionFeedback.kind === 'error'"
        code="Users"
        :title="actionFeedback.title"
        :description="actionFeedback.description"
      />

      <EmptyState
        v-if="!users.length"
        title="No users match the current filters."
        description="Adjust the email or role filter to widen the admin user-management list."
      />

      <div v-else class="section-card roles-card">
        <div class="roles-card__header">
          <div>
            <h2>User access</h2>
            <p class="text-muted">Monitor onboarding posture, reissue temporary passwords, and adjust role assignments.</p>
          </div>
          <span class="text-muted">Page {{ page }} of {{ totalPages }}</span>
        </div>

        <table class="table">
          <thead>
            <tr>
              <th>User</th>
              <th>Linked employee</th>
              <th>Status</th>
              <th>Credential state</th>
              <th>Current role</th>
              <th>Next role</th>
              <th>Actions</th>
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
                <div>{{ onboardingTitle(user) }}</div>
                <div class="text-muted">{{ onboardingDescription(user) }}</div>
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
                <div class="action-stack">
                  <UButton
                    color="neutral"
                    variant="soft"
                    :loading="busyUserId === user.id"
                    :disabled="busyUserId !== null || !hasPendingRoleChange(user)"
                    @click="void applyRoleChange(user)"
                  >
                    Apply role
                  </UButton>
                  <UButton
                    v-if="canResendTemporaryPassword(user)"
                    color="primary"
                    variant="soft"
                    :loading="busyUserId === user.id"
                    :disabled="busyUserId !== null"
                    @click="void resendInvite(user)"
                  >
                    Resend invite
                  </UButton>
                </div>
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

.roles-card,
.create-user-card {
  padding: 1rem;
}

.roles-card__header,
.create-user-card__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1rem;
}

.roles-card__header h2,
.create-user-card__header h2 {
  margin: 0;
  font-size: 1rem;
}

.roles-card__header p,
.create-user-card__header p {
  margin: 0.35rem 0 0;
}

.create-user-card__grid,
.filters {
  display: grid;
  gap: 0.75rem;
}

.create-user-card__grid {
  grid-template-columns: repeat(4, minmax(0, 1fr));
}

.filters {
  grid-template-columns: minmax(0, 1.5fr) 220px auto;
  padding: 1rem;
}

.field {
  display: grid;
  gap: 0.5rem;
}

.field--summary {
  align-content: end;
}

.field__input,
.filters__input {
  min-height: 44px;
  padding: 0 0.85rem;
  border: 1px solid var(--panel-border);
  border-radius: 14px;
  background: var(--panel-bg-strong);
  color: var(--text-main);
}

.field__summary {
  display: grid;
  gap: 0.2rem;
  min-height: 44px;
  padding: 0.75rem 0.85rem;
  border: 1px solid var(--panel-border);
  border-radius: 14px;
  background: var(--panel-bg-strong);
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

.create-user-card__actions,
.table-card__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-top: 1rem;
}

.table-card__pagination,
.action-stack {
  display: flex;
  gap: 0.75rem;
}

.action-stack {
  flex-wrap: wrap;
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

@media (max-width: 1200px) {
  .create-user-card__grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 1100px) {
  .filters,
  .create-user-card__grid {
    grid-template-columns: 1fr;
  }

  .roles-card__header,
  .create-user-card__header,
  .create-user-card__actions,
  .table-card__footer {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
