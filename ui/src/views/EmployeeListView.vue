<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import EmptyState from '@/components/shared/EmptyState.vue'
import LoadingSkeleton from '@/components/shared/LoadingSkeleton.vue'
import PageHeader from '@/components/app/PageHeader.vue'
import { useSessionStore } from '@/stores/session'

const session = useSessionStore()
const search = ref('')
const status = ref<'All' | 'Active' | 'Inactive'>('All')
const page = ref(1)
const pageSize = ref(4)
const includePrimaryAddress = ref(true)
const loading = ref(true)

onMounted(() => {
  window.setTimeout(() => {
    loading.value = false
  }, 350)
})

const filteredRows = computed(() => {
  return session.employeeRows.filter((employee) => {
    const matchesSearch =
      !search.value ||
      `${employee.firstName} ${employee.lastName}`.toLowerCase().includes(search.value.toLowerCase()) ||
      employee.email.toLowerCase().includes(search.value.toLowerCase())
    const matchesStatus = status.value === 'All' || employee.status === status.value

    return matchesSearch && matchesStatus
  })
})

const totalPages = computed(() => Math.max(1, Math.ceil(filteredRows.value.length / pageSize.value)))
const pagedRows = computed(() => {
  const start = (page.value - 1) * pageSize.value
  return filteredRows.value.slice(start, start + pageSize.value)
})

function resetFilters() {
  search.value = ''
  status.value = 'All'
  page.value = 1
}
</script>

<template>
  <section class="view-stack">
    <PageHeader
      title="Employees"
      description="The first real data page is mock-backed here, but already shaped around the approved backend contract."
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

      <EmptyState
        v-if="!pagedRows.length"
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
            <tr v-for="employee in pagedRows" :key="employee.id">
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
