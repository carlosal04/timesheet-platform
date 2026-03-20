<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ConfirmDialog from '@/components/shared/ConfirmDialog.vue'
import PageHeader from '@/components/app/PageHeader.vue'
import ProblemStatePanel from '@/components/shared/ProblemStatePanel.vue'
import { findEmployee } from '@/mocks/ems'
import { deleteEmployeeAddress, listEmployeeAddresses, setEmployeeAddressPrimary } from '@/services/api/addresses'
import { getEmployeeById } from '@/services/api/employees'
import { isApiProblemError, isProblemStatus } from '@/services/api/http'
import { useSessionStore } from '@/stores/session'
import type { EmployeeAddress } from '@/types/ems'

const route = useRoute()
const router = useRouter()
const session = useSessionStore()
const fallbackEmployee = computed(() => findEmployee(String(route.params.id)))

const employeeName = ref('')
const addresses = ref<EmployeeAddress[]>([])
const loading = ref(true)
const actionBusyId = ref<string | null>(null)
const addressPendingDelete = ref<string | null>(null)
const usingMockFallback = ref(false)
const problem = ref<null | { code: string; title: string; description: string }>(null)

async function loadAddresses() {
  loading.value = true
  problem.value = null

  try {
    const [employee, employeeAddresses] = await Promise.all([
      getEmployeeById(String(route.params.id)),
      listEmployeeAddresses(String(route.params.id)),
    ])

    employeeName.value = `${employee.firstName} ${employee.lastName}`
    addresses.value = employeeAddresses
    usingMockFallback.value = false
  } catch (error) {
    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      await router.replace({ name: 'login', query: { redirect: route.fullPath } })
      return
    }

    if (isProblemStatus(error, 404) && fallbackEmployee.value) {
      employeeName.value = `${fallbackEmployee.value.firstName} ${fallbackEmployee.value.lastName}`
      addresses.value = fallbackEmployee.value.addresses.filter((address) => !address.deletedAtUtc)
      usingMockFallback.value = true
      return
    }

    employeeName.value = ''
    addresses.value = []
    usingMockFallback.value = false

    if (isApiProblemError(error)) {
      problem.value = {
        code: String(error.status || 404),
        title: error.problem.title ?? 'Address management unavailable',
        description: error.problem.detail ?? 'Address management needs a visible employee record.',
      }
      return
    }

    problem.value = {
      code: '404',
      title: 'Address management unavailable',
      description: 'Address management needs a visible employee record.',
    }
  } finally {
    loading.value = false
  }
}

async function handleSetPrimary(addressId: string) {
  if (usingMockFallback.value) {
    return
  }

  actionBusyId.value = addressId
  problem.value = null

  try {
    await setEmployeeAddressPrimary(String(route.params.id), addressId)
    await loadAddresses()
  } catch (error) {
    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      await router.replace({ name: 'login', query: { redirect: route.fullPath } })
      return
    }

    if (isApiProblemError(error)) {
      problem.value = {
        code: String(error.status || 409),
        title: error.problem.title ?? 'Unable to change primary address',
        description: error.problem.detail ?? 'The selected primary address could not be updated.',
      }
      return
    }

    problem.value = {
      code: '500',
      title: 'Unable to change primary address',
      description: 'The selected primary address could not be updated.',
    }
  } finally {
    actionBusyId.value = null
  }
}

async function confirmDelete() {
  if (!addressPendingDelete.value || usingMockFallback.value) {
    addressPendingDelete.value = null
    return
  }

  actionBusyId.value = addressPendingDelete.value
  problem.value = null

  try {
    await deleteEmployeeAddress(String(route.params.id), addressPendingDelete.value)
    await loadAddresses()
  } catch (error) {
    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      await router.replace({ name: 'login', query: { redirect: route.fullPath } })
      return
    }

    if (isApiProblemError(error)) {
      problem.value = {
        code: String(error.status || 409),
        title: error.problem.title ?? 'Unable to delete address',
        description: error.problem.detail ?? 'The address could not be deleted.',
      }
      return
    }

    problem.value = {
      code: '500',
      title: 'Unable to delete address',
      description: 'The address could not be deleted.',
    }
  } finally {
    addressPendingDelete.value = null
    actionBusyId.value = null
  }
}

watch(
  () => route.params.id,
  () => {
    void loadAddresses()
  },
  { immediate: true },
)
</script>

<template>
  <div v-if="loading" class="section-card">
    <p class="text-muted">Loading employee addresses...</p>
  </div>

  <ProblemStatePanel
    v-else-if="problem"
    :code="problem.code"
    :title="problem.title"
    :description="problem.description"
  />

  <section v-else-if="employeeName" class="view-stack">
    <PageHeader
      :title="`${employeeName} addresses`"
      description="Admin-only address management with live primary-address and soft-delete actions."
    >
      <template #actions>
        <UButton disabled>Add address next</UButton>
      </template>
    </PageHeader>

    <ProblemStatePanel
      v-if="usingMockFallback"
      code="Info"
      title="Address write actions are available only on real employee records."
      description="This fallback route is still rendering fixture data. The live address actions are active when the employee comes from the EMS API."
    />

    <div class="card-grid">
      <article v-for="address in addresses" :key="address.id" class="section-card address-card">
        <div class="address-card__header">
          <span v-if="address.isPrimary" class="status-pill status-pill--brand">Primary</span>
          <span class="text-muted">{{ address.addressType }}</span>
        </div>
        <p class="text-muted">
          {{ address.line1 }}<span v-if="address.line2">, {{ address.line2 }}</span><br />
          {{ address.city }}, {{ address.state }} {{ address.zipCode }}
        </p>
        <div class="address-card__actions">
          <UButton color="neutral" variant="soft" disabled>Edit next</UButton>
          <UButton
            color="primary"
            variant="soft"
            :disabled="usingMockFallback || address.isPrimary"
            :loading="actionBusyId === address.id"
            @click="handleSetPrimary(address.id)"
          >
            Set primary
          </UButton>
          <UButton
            color="error"
            variant="soft"
            :disabled="usingMockFallback"
            :loading="actionBusyId === address.id"
            @click="addressPendingDelete = address.id"
          >
            Delete
          </UButton>
        </div>
      </article>
    </div>

    <ConfirmDialog
      :model-value="Boolean(addressPendingDelete)"
      title="Delete address?"
      description="This performs the approved soft-delete flow for the selected employee address."
      @update:model-value="addressPendingDelete = null"
      @confirm="confirmDelete"
    />
  </section>

  <ProblemStatePanel
    v-else
    code="404"
    title="Employee not found"
    description="Address management needs a visible employee record."
  />
</template>

<style scoped>
.view-stack,
.card-grid {
  display: grid;
  gap: 1rem;
}

.card-grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.address-card {
  display: grid;
  gap: 1rem;
  padding: 1rem;
}

.address-card__header,
.address-card__actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  flex-wrap: wrap;
}

@media (max-width: 900px) {
  .card-grid {
    grid-template-columns: 1fr;
  }
}
</style>
