<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ConfirmDialog from '@/components/shared/ConfirmDialog.vue'
import PageHeader from '@/components/app/PageHeader.vue'
import ProblemStatePanel from '@/components/shared/ProblemStatePanel.vue'
import ValidationSummary from '@/components/shared/ValidationSummary.vue'
import { findEmployee } from '@/mocks/ems'
import {
  createEmployeeAddress,
  deleteEmployeeAddress,
  listEmployeeAddresses,
  setEmployeeAddressPrimary,
  updateEmployeeAddress,
} from '@/services/api/addresses'
import { getEmployeeById } from '@/services/api/employees'
import { isApiProblemError, isProblemStatus } from '@/services/api/http'
import { useSessionStore } from '@/stores/session'
import type { EmployeeAddress } from '@/types/ems'
import type { AddressWriteInput } from '@/services/api/addresses'

const route = useRoute()
const router = useRouter()
const session = useSessionStore()
const fallbackEmployee = computed(() => findEmployee(String(route.params.id)))

const employeeName = ref('')
const addresses = ref<EmployeeAddress[]>([])
const loading = ref(true)
const actionBusyId = ref<string | null>(null)
const addressPendingDelete = ref<string | null>(null)
const showingForm = ref(false)
const editingAddressId = ref<string | null>(null)
const formSubmitted = ref(false)
const formLoading = ref(false)
const validationMessages = ref<string[]>([])
const usingMockFallback = ref(false)
const pageProblem = ref<null | { code: string; title: string; description: string }>(null)
const formProblem = ref<null | { code: string; title: string; description: string }>(null)

const form = reactive({
  addressType: 'Home' as EmployeeAddress['addressType'],
  isPrimary: false,
  line1: '',
  line2: '',
  city: '',
  state: '',
  zipCode: '',
  countryCode: 'US',
})

const isEditMode = computed(() => editingAddressId.value !== null)

const localValidationMessages = computed(() => {
  const messages: string[] = []

  if (!form.addressType) {
    messages.push('Address type is required.')
  }

  if (!form.line1.trim()) {
    messages.push('Line 1 is required.')
  }

  if (!form.city.trim()) {
    messages.push('City is required.')
  }

  if (!form.state.trim()) {
    messages.push('State is required.')
  }

  if (!form.zipCode.trim()) {
    messages.push('ZIP code is required.')
  }

  if (form.countryCode.trim().length !== 2) {
    messages.push('Country code must be two characters.')
  }

  return messages
})

function assignForm(address: EmployeeAddress | null) {
  form.addressType = address?.addressType ?? 'Home'
  form.isPrimary = address?.isPrimary ?? addresses.value.length === 0
  form.line1 = address?.line1 ?? ''
  form.line2 = address?.line2 ?? ''
  form.city = address?.city ?? ''
  form.state = address?.state ?? ''
  form.zipCode = address?.zipCode ?? ''
  form.countryCode = address?.countryCode ?? 'US'
}

function openCreateForm() {
  editingAddressId.value = null
  formSubmitted.value = false
  validationMessages.value = []
  formProblem.value = null
  assignForm(null)
  showingForm.value = true
}

function openEditForm(address: EmployeeAddress) {
  editingAddressId.value = address.id
  formSubmitted.value = false
  validationMessages.value = []
  formProblem.value = null
  assignForm(address)
  showingForm.value = true
}

function closeForm() {
  showingForm.value = false
  editingAddressId.value = null
  formSubmitted.value = false
  validationMessages.value = []
  formProblem.value = null
  formLoading.value = false
}

function extractValidationMessages(error: unknown) {
  if (!isApiProblemError(error)) {
    return []
  }

  const errors = error.problem.errors
  if (typeof errors !== 'object' || errors === null) {
    return []
  }

  return Object.values(errors)
    .flatMap((value) => (Array.isArray(value) ? value : []))
    .filter((value): value is string => typeof value === 'string')
}

async function submitForm() {
  if (usingMockFallback.value) {
    return
  }

  formSubmitted.value = true
  validationMessages.value = localValidationMessages.value
  formProblem.value = null

  if (validationMessages.value.length) {
    return
  }

  formLoading.value = true

  try {
    const payload = {
      addressType: form.addressType,
      isPrimary: form.isPrimary,
      line1: form.line1.trim(),
      line2: form.line2.trim() || null,
      city: form.city.trim(),
      state: form.state.trim(),
      zipCode: form.zipCode.trim(),
      countryCode: form.countryCode.trim().toUpperCase(),
    } satisfies AddressWriteInput

    if (isEditMode.value && editingAddressId.value) {
      await updateEmployeeAddress(String(route.params.id), editingAddressId.value, payload)
    } else {
      await createEmployeeAddress(String(route.params.id), payload)
    }

    closeForm()
    await loadAddresses()
  } catch (error) {
    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      await router.replace({ name: 'login', query: { redirect: route.fullPath } })
      return
    }

    const backendMessages = extractValidationMessages(error)
    if (backendMessages.length) {
      validationMessages.value = backendMessages
      return
    }

    if (isApiProblemError(error)) {
      formProblem.value = {
        code: String(error.status || 409),
        title: error.problem.title ?? 'Unable to save address',
        description: error.problem.detail ?? 'The address could not be saved.',
      }
      return
    }

    formProblem.value = {
      code: '500',
      title: 'Unable to save address',
      description: 'The address could not be saved.',
    }
  } finally {
    formLoading.value = false
  }
}

async function loadAddresses() {
  loading.value = true
  pageProblem.value = null

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
      pageProblem.value = {
        code: String(error.status || 404),
        title: error.problem.title ?? 'Address management unavailable',
        description: error.problem.detail ?? 'Address management needs a visible employee record.',
      }
      return
    }

    pageProblem.value = {
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
  pageProblem.value = null

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
      pageProblem.value = {
        code: String(error.status || 409),
        title: error.problem.title ?? 'Unable to change primary address',
        description: error.problem.detail ?? 'The selected primary address could not be updated.',
      }
      return
    }

    pageProblem.value = {
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
  pageProblem.value = null

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
      pageProblem.value = {
        code: String(error.status || 409),
        title: error.problem.title ?? 'Unable to delete address',
        description: error.problem.detail ?? 'The address could not be deleted.',
      }
      return
    }

    pageProblem.value = {
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
    v-else-if="pageProblem"
    :code="pageProblem.code"
    :title="pageProblem.title"
    :description="pageProblem.description"
  />

  <section v-else-if="employeeName" class="view-stack">
    <PageHeader
      :title="`${employeeName} addresses`"
      description="Manage employee addresses with the create, primary, edit, and soft-delete actions allowed for Admin and HR."
    >
      <template #actions>
        <UButton :disabled="usingMockFallback" @click="openCreateForm">Add address</UButton>
      </template>
    </PageHeader>

    <ProblemStatePanel
      v-if="usingMockFallback"
      code="Info"
      title="Address write actions are available only on real employee records."
      description="This screen is showing fallback reference data because the employee was not available from the API. Address changes require a live employee record."
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
          <UButton
            color="neutral"
            variant="soft"
            :disabled="usingMockFallback"
            @click="openEditForm(address)"
          >
            Edit
          </UButton>
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

    <div v-if="showingForm" class="form-modal">
      <div class="form-modal__backdrop" @click="closeForm"></div>
      <div class="form-modal__panel section-card">
        <div class="form-modal__header">
          <div>
            <h3>{{ isEditMode ? 'Edit address' : 'Add address' }}</h3>
            <p class="text-muted">
              {{ isEditMode ? 'Update the selected employee address.' : 'Create a new address for this employee.' }}
            </p>
          </div>
        </div>

        <ProblemStatePanel
          v-if="formProblem"
          :code="formProblem.code"
          :title="formProblem.title"
          :description="formProblem.description"
        />

        <ValidationSummary :messages="formSubmitted ? validationMessages : []" />

        <form class="form-grid" @submit.prevent="submitForm">
          <div>
            <label>Address type</label>
            <select v-model="form.addressType" class="form-input">
              <option value="Home">Home</option>
              <option value="Mailing">Mailing</option>
              <option value="EmergencyContact">Emergency contact</option>
              <option value="Other">Other</option>
            </select>
          </div>
          <div class="checkbox-field">
            <label class="checkbox-label">
              <input v-model="form.isPrimary" type="checkbox" />
              Set as primary
            </label>
          </div>
          <div class="form-grid__full">
            <label>Line 1</label>
            <input v-model="form.line1" class="form-input" />
          </div>
          <div class="form-grid__full">
            <label>Line 2</label>
            <input v-model="form.line2" class="form-input" />
          </div>
          <div>
            <label>City</label>
            <input v-model="form.city" class="form-input" />
          </div>
          <div>
            <label>State</label>
            <input v-model="form.state" class="form-input" />
          </div>
          <div>
            <label>ZIP code</label>
            <input v-model="form.zipCode" class="form-input" />
          </div>
          <div>
            <label>Country code</label>
            <input v-model="form.countryCode" class="form-input" maxlength="2" />
          </div>

          <div class="form-actions form-grid__full">
            <UButton type="button" color="neutral" variant="soft" @click="closeForm">Cancel</UButton>
            <UButton type="submit" :loading="formLoading">
              {{ isEditMode ? 'Save changes' : 'Create address' }}
            </UButton>
          </div>
        </form>
      </div>
    </div>
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

.form-modal {
  position: fixed;
  inset: 0;
  z-index: 50;
}

.form-modal__backdrop {
  position: absolute;
  inset: 0;
  background: rgba(2, 6, 23, 0.5);
}

.form-modal__panel {
  position: relative;
  width: min(760px, calc(100% - 32px));
  margin: 6vh auto 0;
  padding: 1.5rem;
}

.form-modal__header {
  margin-bottom: 1rem;
}

.form-modal__header h3 {
  margin: 0;
}

.form-modal__header p {
  margin: 0.35rem 0 0;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem;
}

.form-grid__full {
  grid-column: 1 / -1;
}

.form-input {
  min-height: 44px;
  width: 100%;
  padding: 0 0.85rem;
  border: 1px solid var(--panel-border);
  border-radius: 14px;
  background: var(--panel-bg-strong);
  color: var(--text-main);
}

.checkbox-field {
  display: flex;
  align-items: flex-end;
}

.checkbox-label {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
}

@media (max-width: 900px) {
  .card-grid {
    grid-template-columns: 1fr;
  }

  .form-grid {
    grid-template-columns: 1fr;
  }
}
</style>
