<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import PageHeader from '@/components/app/PageHeader.vue'
import ProblemStatePanel from '@/components/shared/ProblemStatePanel.vue'
import ValidationSummary from '@/components/shared/ValidationSummary.vue'
import { createEmployee, getEmployeeById, updateEmployee } from '@/services/api/employees'
import { isApiProblemError, isProblemStatus } from '@/services/api/http'
import { useSessionStore } from '@/stores/session'
import type { EmployeeRecord } from '@/types/ems'
import type { EmployeeWriteInput } from '@/services/api/employees'

const route = useRoute()
const router = useRouter()
const session = useSessionStore()
const isEditMode = computed(() => route.name === 'employee-edit')
const existingEmployee = ref<EmployeeRecord | null>(null)
const loading = ref(false)
const submitted = ref(false)
const validationMessages = ref<string[]>([])
const formProblem = ref<null | { code: string; title: string; description: string }>(null)

const form = reactive({
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  dateOfBirth: '',
  hireDate: '',
  status: 'Active' as EmployeeRecord['status'],
})

function assignForm(employee: EmployeeRecord | null) {
  form.firstName = employee?.firstName ?? ''
  form.lastName = employee?.lastName ?? ''
  form.email = employee?.email ?? ''
  form.phone = employee?.phone ?? ''
  form.dateOfBirth = employee?.dateOfBirth ?? ''
  form.hireDate = employee?.hireDate ?? ''
  form.status = employee?.status ?? 'Active'
}

const localValidationMessages = computed(() => {
  const messages: string[] = []

  if (!form.firstName.trim()) {
    messages.push('First name is required.')
  }

  if (!form.lastName.trim()) {
    messages.push('Last name is required.')
  }

  if (!form.email.includes('@')) {
    messages.push('Email must be valid.')
  }

  if (form.dateOfBirth && form.hireDate) {
    const dob = new Date(form.dateOfBirth)
    const hire = new Date(form.hireDate)
    const ageAtHire = hire.getFullYear() - dob.getFullYear()

    if (ageAtHire < 21) {
      messages.push('Employee must be at least 21 years old.')
    }

    const minHire = new Date(dob)
    minHire.setFullYear(minHire.getFullYear() + 14)

    if (hire < minHire) {
      messages.push('Hire date must be at least 14 years after date of birth.')
    }
  }

  return messages
})

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

async function loadEmployee() {
  if (!isEditMode.value) {
    existingEmployee.value = null
    assignForm(null)
    return
  }

  loading.value = true
  formProblem.value = null
  validationMessages.value = []

  try {
    const employee = await getEmployeeById(String(route.params.id))
    existingEmployee.value = employee
    assignForm(employee)
  } catch (error) {
    existingEmployee.value = null

    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      await router.replace({ name: 'login', query: { redirect: route.fullPath } })
      return
    }

    if (isApiProblemError(error)) {
      formProblem.value = {
        code: String(error.status || 404),
        title: error.problem.title ?? 'Employee not found',
        description: error.problem.detail ?? 'The employee you are trying to edit does not exist.',
      }
      return
    }

    formProblem.value = {
      code: '500',
      title: 'Employee not found',
      description: 'The employee you are trying to edit does not exist.',
    }
  } finally {
    loading.value = false
  }
}

async function submitForm() {
  submitted.value = true
  validationMessages.value = localValidationMessages.value
  formProblem.value = null

  if (validationMessages.value.length) {
    return
  }

  loading.value = true

  try {
    const payload = {
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
      email: form.email.trim(),
      phone: form.phone.trim(),
      dateOfBirth: form.dateOfBirth,
      hireDate: form.hireDate,
      status: form.status,
    } satisfies EmployeeWriteInput

    const employeeId = isEditMode.value
      ? await updateEmployee(String(route.params.id), payload)
      : await createEmployee(payload)

    await router.push({ name: 'employee-detail', params: { id: employeeId } })
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
        title: error.problem.title ?? 'Employee save failed',
        description: error.problem.detail ?? 'The employee could not be saved.',
      }
      return
    }

    formProblem.value = {
      code: '500',
      title: 'Employee save failed',
      description: 'The employee could not be saved.',
    }
  } finally {
    loading.value = false
  }
}

async function cancelForm() {
  if (isEditMode.value) {
    await router.push({ name: 'employee-detail', params: { id: String(route.params.id) } })
    return
  }

  await router.push({ name: 'employees' })
}

watch(
  [isEditMode, () => route.params.id],
  () => {
    void loadEmployee()
  },
  { immediate: true },
)
</script>

<template>
  <div v-if="loading && isEditMode && !existingEmployee" class="section-card">
    <p class="text-muted">Loading employee form...</p>
  </div>

  <ProblemStatePanel
    v-else-if="formProblem && isEditMode && !existingEmployee"
    :code="formProblem.code"
    :title="formProblem.title"
    :description="formProblem.description"
  />

  <section v-else class="view-stack">
    <PageHeader
      :title="isEditMode ? 'Edit employee' : 'Create employee'"
      description="Create or update employee records with the validation and write posture allowed for Admin and HR."
    />

    <form class="section-card form-card" @submit.prevent="submitForm">
      <ProblemStatePanel
        v-if="formProblem && (!isEditMode || existingEmployee)"
        :code="formProblem.code"
        :title="formProblem.title"
        :description="formProblem.description"
      />

      <ValidationSummary :messages="submitted ? validationMessages : []" />

      <div class="form-grid">
        <div><label>First name</label><input v-model="form.firstName" class="form-input" /></div>
        <div><label>Last name</label><input v-model="form.lastName" class="form-input" /></div>
        <div><label>Email</label><input v-model="form.email" class="form-input" type="email" /></div>
        <div><label>Phone</label><input v-model="form.phone" class="form-input" /></div>
        <div><label>Date of birth</label><input v-model="form.dateOfBirth" class="form-input" type="date" /></div>
        <div><label>Hire date</label><input v-model="form.hireDate" class="form-input" type="date" /></div>
        <div><label>Status</label><select v-model="form.status" class="form-input"><option>Active</option><option>Inactive</option></select></div>
      </div>

      <div class="form-actions">
        <UButton type="button" color="neutral" variant="soft" @click="cancelForm">Cancel</UButton>
        <UButton type="submit" :loading="loading">
          {{ isEditMode ? 'Save changes' : 'Create employee' }}
        </UButton>
      </div>
    </form>
  </section>
</template>

<style scoped>
.view-stack {
  display: grid;
  gap: 1rem;
}

.form-card {
  display: grid;
  gap: 1rem;
  padding: 1.2rem;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem;
}

label {
  display: block;
  margin-bottom: 0.5rem;
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

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
}

@media (max-width: 900px) {
  .form-grid {
    grid-template-columns: 1fr;
  }
}
</style>
