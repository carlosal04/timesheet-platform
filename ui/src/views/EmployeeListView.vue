<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import EmptyState from '@/components/shared/EmptyState.vue'
import LoadingSkeleton from '@/components/shared/LoadingSkeleton.vue'
import PageHeader from '@/components/app/PageHeader.vue'
import ProblemStatePanel from '@/components/shared/ProblemStatePanel.vue'
import { listEmployees } from '@/services/api/employees'
import { isApiProblemError, isProblemStatus } from '@/services/api/http'
import { useSessionStore } from '@/stores/session'
import type { EmployeeListRow } from '@/types/ems'

const session = useSessionStore()
const route = useRoute()
const router = useRouter()
const search = ref('')
const status = ref<'All' | 'Active' | 'Inactive'>('All')
const page = ref(1)
const pageSize = ref(4)
const includePrimaryAddress = ref(true)
const loading = ref(true)
const totalCount = ref(0)
const rows = ref<EmployeeListRow[]>([])
const problem = ref<null | { code: string; title: string; description: string }>(null)
let requestSequence = 0

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

async function loadEmployees() {
  const sequence = ++requestSequence
  loading.value = true
  problem.value = null

  try {
    const result = await listEmployees({
      page: page.value,
      pageSize: pageSize.value,
      name: search.value.trim() || undefined,
      status: status.value === 'All' ? undefined : status.value,
      includePrimaryAddress: includePrimaryAddress.value,
    })

    if (sequence !== requestSequence) {
      return
    }

    rows.value = result.items
    totalCount.value = result.totalCount
    page.value = result.page
    pageSize.value = result.pageSize
  } catch (error) {
    if (sequence !== requestSequence) {
      return
    }

    rows.value = []
    totalCount.value = 0

    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      await router.replace({ name: 'login', query: { redirect: route.fullPath } })
      return
    }

    if (isApiProblemError(error)) {
      problem.value = {
        code: String(error.status || 500),
        title: error.problem.title ?? 'Employee list unavailable',
        description: error.problem.detail ?? 'The employee list could not be loaded from the EMS API.',
      }
      return
    }

    problem.value = {
      code: '500',
      title: 'Employee list unavailable',
      description: 'The employee list could not be loaded from the EMS API.',
    }
  } finally {
    if (sequence === requestSequence) {
      loading.value = false
    }
  }
}

function resetFilters() {
  search.value = ''
  status.value = 'All'
  page.value = 1
}

watch([search, status, includePrimaryAddress], () => {
  page.value = 1
})

watch([search, status, page, pageSize, includePrimaryAddress], () => {
  void loadEmployees()
})

onMounted(() => {
  void loadEmployees()
})
</script>

<template>
  <section class="view-stack">
    <PageHeader
      title="Employees"
      description="The employee list now loads from the real EMS API while the rest of the placeholder screens remain mock-backed."
    >
      <template #actions>
        <label class="toggle text-muted">
          <input v-model="includePrimaryAddress" type="checkbox" />
          Include primary address
        </label>
        <RouterLink v-if="session.roleCode === 'Admin'" to="/employees/new">
          <UButton>New employee</UButton>
        </RouterLink>
      </template>
    </PageHeader>

    <LoadingSkeleton v-if="loading" />

    <template v-else>
      <div class="section-card filters">
        <input v-model="search" class="filters__input" placeholder="Search by name or email" />
        <select v-model="status" class="filters__input">
          <option>All</option>
          <option>Active</option>
          <option>Inactive</option>
        </select>
        <button class="filters__button" type="button" @click="resetFilters">Reset</button>
      </div>

      <ProblemStatePanel
        v-if="problem"
        :code="problem.code"
        :title="problem.title"
        :description="problem.description"
      />

      <EmptyState
        v-else-if="!rows.length"
        title="No employees match the current filters."
        description="Adjust the name or status filter to widen the result set."
      />

      <div v-else class="section-card table-card">
        <table class="table">
          <thead>
            <tr>
              <th>Employee</th>
              <th>Status</th>
              <th>Hire date</th>
              <th v-if="includePrimaryAddress">Primary address</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="employee in rows" :key="employee.id">
              <td>
                <RouterLink class="table__link" :to="`/employees/${employee.id}`">
                  <strong>{{ employee.firstName }} {{ employee.lastName }}</strong>
                </RouterLink>
                <div class="text-muted">{{ employee.email }}</div>
              </td>
              <td>
                <span
                  class="status-pill"
                  :class="employee.status === 'Active' ? 'status-pill--success' : 'status-pill--warning'"
                >
                  {{ employee.status }}
                </span>
              </td>
              <td>{{ employee.hireDate }}</td>
              <td v-if="includePrimaryAddress">
                {{
                  employee.primaryAddress
                    ? `${employee.primaryAddress.city}, ${employee.primaryAddress.state}`
                    : 'No primary address'
                }}
              </td>
            </tr>
          </tbody>
        </table>

        <div class="table-card__footer">
          <span class="text-muted">Page {{ page }} of {{ totalPages }}</span>
          <div class="table-card__pagination">
            <UButton color="neutral" variant="soft" :disabled="page === 1" @click="page -= 1">Previous</UButton>
            <UButton
              color="neutral"
              variant="soft"
              :disabled="page === totalPages"
              @click="page += 1"
            >
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

.filters__button {
  min-height: 44px;
  padding: 0 1rem;
  border: 1px solid var(--panel-border);
  border-radius: 14px;
  background: transparent;
  color: var(--text-main);
}

.table-card {
  padding: 1rem;
}

.table {
  width: 100%;
  border-collapse: collapse;
}

.table th {
  padding: 0.75rem 0;
  text-align: left;
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

.table__link {
  color: var(--primary);
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

.toggle {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
}

@media (max-width: 900px) {
  .filters {
    grid-template-columns: 1fr;
  }
}
</style>
