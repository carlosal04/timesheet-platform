<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import PageHeader from '@/components/app/PageHeader.vue'
import ProblemStatePanel from '@/components/shared/ProblemStatePanel.vue'
import ValidationSummary from '@/components/shared/ValidationSummary.vue'
import { findEmployee } from '@/mocks/ems'

const route = useRoute()
const existingEmployee = computed(() => {
  if (route.name !== 'employee-edit') {
    return null
  }

  return findEmployee(String(route.params.id))
})

const form = reactive({
  firstName: existingEmployee.value?.firstName ?? '',
  lastName: existingEmployee.value?.lastName ?? '',
  email: existingEmployee.value?.email ?? '',
  phone: existingEmployee.value?.phone ?? '',
  dateOfBirth: existingEmployee.value?.dateOfBirth ?? '',
  hireDate: existingEmployee.value?.hireDate ?? '',
  status: existingEmployee.value?.status ?? 'Active',
})
const submitted = ref(false)

const validationMessages = computed(() => {
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

function submitForm() {
  submitted.value = true
}
</script>

<template>
  <ProblemStatePanel
    v-if="route.name === 'employee-edit' && !existingEmployee"
    code="404"
    title="Employee not found"
    description="The employee you are trying to edit does not exist in the current mock data set."
  />

  <section v-else class="view-stack">
    <PageHeader
      :title="route.name === 'employee-edit' ? 'Edit employee' : 'Create employee'"
      description="Admin-only form shaped around the approved validation rules and response posture."
    />

    <form class="section-card form-card" @submit.prevent="submitForm">
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
        <UButton color="neutral" variant="soft">Cancel</UButton>
        <UButton type="submit">{{ route.name === 'employee-edit' ? 'Save changes' : 'Create employee' }}</UButton>
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
