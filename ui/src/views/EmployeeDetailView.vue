<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import PageHeader from '@/components/app/PageHeader.vue'
import ConfirmDialog from '@/components/shared/ConfirmDialog.vue'
import ProblemStatePanel from '@/components/shared/ProblemStatePanel.vue'
import { findEmployee } from '@/mocks/ems'
import { deleteEmployee, getEmployeeById } from '@/services/api/employees'
import { isApiProblemError, isProblemStatus } from '@/services/api/http'
import { useSessionStore } from '@/stores/session'
import type { EmployeeRecord } from '@/types/ems'

const route = useRoute()
const router = useRouter()
const session = useSessionStore()
const employee = ref<EmployeeRecord | null>(null)
const loading = ref(true)
const problem = ref<null | { code: string; title: string; description: string }>(null)
const showingDeleteConfirm = ref(false)
const usingMockFallback = ref(false)

const fallbackEmployee = computed(() => findEmployee(String(route.params.id)))

async function loadEmployee() {
  loading.value = true
  problem.value = null

  try {
    employee.value = await getEmployeeById(String(route.params.id))
    usingMockFallback.value = false
  } catch (error) {
    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      employee.value = null
      await router.replace({ name: 'login', query: { redirect: route.fullPath } })
      return
    }

    if (isProblemStatus(error, 404) && fallbackEmployee.value) {
      employee.value = fallbackEmployee.value
      usingMockFallback.value = true
      return
    }

    employee.value = null
    usingMockFallback.value = false

    if (isApiProblemError(error)) {
      problem.value = {
        code: String(error.status || 404),
        title: error.problem.title ?? 'Employee not found',
        description: error.problem.detail ?? 'The requested employee is missing or hidden in the current posture.',
      }
      return
    }

    problem.value = {
      code: '404',
      title: 'Employee not found',
      description: 'The requested employee is missing or hidden in the current posture.',
    }
  } finally {
    loading.value = false
  }
}

async function confirmDelete() {
  if (!employee.value || usingMockFallback.value) {
    return
  }

  loading.value = true
  problem.value = null

  try {
    await deleteEmployee(employee.value.id)
    await router.replace({ name: 'employees' })
  } catch (error) {
    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      await router.replace({ name: 'login', query: { redirect: route.fullPath } })
      return
    }

    if (isApiProblemError(error)) {
      problem.value = {
        code: String(error.status || 409),
        title: error.problem.title ?? 'Unable to delete employee',
        description: error.problem.detail ?? 'The employee could not be deleted.',
      }
      return
    }

    problem.value = {
      code: '500',
      title: 'Unable to delete employee',
      description: 'The employee could not be deleted.',
    }
  } finally {
    showingDeleteConfirm.value = false
    loading.value = false
  }
}

watch(
  () => route.params.id,
  () => {
    void loadEmployee()
  },
  { immediate: true },
)
</script>

<template>
  <div v-if="loading" class="section-card">
    <p class="text-muted">Loading employee details...</p>
  </div>

  <ProblemStatePanel
    v-else-if="problem"
    :code="problem.code"
    :title="problem.title"
    :description="problem.description"
  />

  <section v-else-if="employee" class="view-stack">
    <PageHeader
      :title="`${employee.firstName} ${employee.lastName}`"
      description="Employee profile, active addresses, and the admin path into edit and address management."
    >
      <template #actions>
        <RouterLink to="/employees">
          <UButton color="neutral" variant="soft">Back to list</UButton>
        </RouterLink>
        <RouterLink v-if="!usingMockFallback" :to="`/employees/${employee.id}/edit`">
          <UButton>Edit employee</UButton>
        </RouterLink>
        <UButton v-if="!usingMockFallback" color="error" variant="soft" @click="showingDeleteConfirm = true">
          Delete employee
        </UButton>
      </template>
    </PageHeader>

    <div class="detail-grid">
      <div class="section-card panel">
        <h3>Identity and employment</h3>
        <dl class="definition-list">
          <div><dt>Email</dt><dd>{{ employee.email }}</dd></div>
          <div><dt>Phone</dt><dd>{{ employee.phone }}</dd></div>
          <div><dt>Date of birth</dt><dd>{{ employee.dateOfBirth }}</dd></div>
          <div><dt>Hire date</dt><dd>{{ employee.hireDate }}</dd></div>
          <div><dt>Status</dt><dd>{{ employee.status }}</dd></div>
        </dl>
      </div>

      <div class="section-card panel">
        <div class="panel__header">
          <h3>Addresses</h3>
          <RouterLink :to="`/employees/${employee.id}/addresses`">
            <UButton color="neutral" variant="soft">Manage addresses</UButton>
          </RouterLink>
        </div>
        <div class="address-grid">
          <article v-for="address in employee.addresses" :key="address.id" class="address-card">
            <span v-if="address.isPrimary" class="status-pill status-pill--brand">Primary</span>
            <h4>{{ address.addressType }}</h4>
            <p class="text-muted">
              {{ address.line1 }}<span v-if="address.line2">, {{ address.line2 }}</span><br />
              {{ address.city }}, {{ address.state }} {{ address.zipCode }}
            </p>
          </article>
        </div>
      </div>
    </div>
  </section>

  <ProblemStatePanel
    v-else
    code="404"
    title="Employee not found"
    description="The requested employee is missing or hidden in the current posture."
  />

  <ConfirmDialog
    v-model="showingDeleteConfirm"
    title="Delete employee"
    description="This performs the approved soft-delete path and removes the employee from active reads."
    @confirm="confirmDelete"
  />
</template>

<style scoped>
.view-stack,
.detail-grid,
.address-grid {
  display: grid;
  gap: 1rem;
}

.detail-grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.panel {
  padding: 1.2rem;
}

.panel__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.definition-list {
  display: grid;
  gap: 0.75rem;
  margin-top: 1rem;
}

.definition-list div {
  display: grid;
  gap: 0.2rem;
}

dt {
  font-size: 0.8rem;
  font-weight: 700;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.address-grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
  margin-top: 1rem;
}

.address-card {
  padding: 1rem;
  border: 1px solid var(--panel-border);
  border-radius: 16px;
  background: var(--panel-bg-strong);
}

.address-card h4 {
  margin: 0.65rem 0 0;
}

@media (max-width: 900px) {
  .detail-grid,
  .address-grid {
    grid-template-columns: 1fr;
  }
}
</style>
