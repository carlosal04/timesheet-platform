<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import ProblemStatePanel from '@/components/shared/ProblemStatePanel.vue'
import ValidationSummary from '@/components/shared/ValidationSummary.vue'
import { changePassword } from '@/services/api/auth'
import { isApiProblemError, isProblemStatus } from '@/services/api/http'
import { useSessionStore } from '@/stores/session'

const router = useRouter()
const session = useSessionStore()
const loading = ref(false)
const submitted = ref(false)
const validationMessages = ref<string[]>([])
const formProblem = ref<null | { code: string; title: string; description: string }>(null)
const form = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
})

const isForcedChange = computed(() => session.mustChangePassword)

const localValidationMessages = computed(() => {
  const messages: string[] = []

  if (!form.currentPassword) {
    messages.push('Current password is required.')
  }

  if (form.newPassword.length < 12) {
    messages.push('New password must be at least 12 characters long.')
  }

  if (form.newPassword !== form.confirmPassword) {
    messages.push('New password and confirmation must match.')
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

async function submitForm() {
  submitted.value = true
  validationMessages.value = localValidationMessages.value
  formProblem.value = null

  if (validationMessages.value.length) {
    return
  }

  loading.value = true

  try {
    await changePassword(form.currentPassword, form.newPassword)
    const refreshed = await session.refreshSession()
    if (!refreshed) {
      await router.replace({ name: 'login' })
      return
    }

    await router.replace(session.defaultAuthenticatedPath)
  } catch (error) {
    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      await router.replace({ name: 'login' })
      return
    }

    const backendMessages = extractValidationMessages(error)
    if (backendMessages.length) {
      validationMessages.value = backendMessages
      return
    }

    if (isApiProblemError(error)) {
      formProblem.value = {
        code: String(error.status || 400),
        title: error.problem.title ?? 'Unable to change password',
        description: error.problem.detail ?? 'The password change request was rejected.',
      }
      return
    }

    formProblem.value = {
      code: '500',
      title: 'Unable to change password',
      description: 'The password change request was rejected.',
    }
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="app-page">
    <section class="page-card auth-layout">
      <div class="auth-layout__hero">
        <span class="status-pill status-pill--brand">{{ isForcedChange ? 'Action required' : 'Security' }}</span>
        <div>
          <h1>{{ isForcedChange ? 'Set your permanent password' : 'Change password' }}</h1>
          <p class="text-muted">
            {{
              isForcedChange
                ? 'Your temporary password worked, but EMS requires a new permanent password before you can open the rest of the workspace.'
                : 'Update your current password to keep your EMS access secure.'
            }}
          </p>
        </div>
      </div>

      <form class="section-card auth-layout__card" @submit.prevent="submitForm">
        <ProblemStatePanel
          v-if="formProblem"
          :code="formProblem.code"
          :title="formProblem.title"
          :description="formProblem.description"
        />

        <ValidationSummary :messages="submitted ? validationMessages : []" />

        <div class="auth-layout__field">
          <label for="current-password">Current password</label>
          <input
            id="current-password"
            v-model="form.currentPassword"
            class="auth-layout__input"
            type="password"
            autocomplete="current-password"
          />
        </div>

        <div class="auth-layout__field">
          <label for="new-password">New password</label>
          <input
            id="new-password"
            v-model="form.newPassword"
            class="auth-layout__input"
            type="password"
            autocomplete="new-password"
          />
        </div>

        <div class="auth-layout__field">
          <label for="confirm-password">Confirm new password</label>
          <input
            id="confirm-password"
            v-model="form.confirmPassword"
            class="auth-layout__input"
            type="password"
            autocomplete="new-password"
          />
        </div>

        <div class="auth-layout__actions">
          <RouterLink v-if="!isForcedChange" class="auth-layout__secondary-link" :to="session.defaultAuthenticatedPath">
            Cancel
          </RouterLink>
          <UButton type="submit" :loading="loading">Save password</UButton>
        </div>
      </form>
    </section>
  </div>
</template>

<style scoped>
.auth-layout {
  display: grid;
  grid-template-columns: minmax(0, 1.1fr) 440px;
  gap: 1.25rem;
  padding: 1.2rem;
}

.auth-layout__hero,
.auth-layout__card {
  display: grid;
  gap: 1rem;
}

.auth-layout__hero {
  align-content: center;
  padding: 1.7rem;
  border-radius: 22px;
  background: linear-gradient(135deg, var(--surface-dark), var(--surface-dark-muted));
  color: var(--surface-dark-text);
}

.auth-layout__hero h1 {
  margin: 1rem 0 0;
  font-size: clamp(2.4rem, 4vw, 4rem);
  line-height: 0.98;
  letter-spacing: -0.05em;
}

.auth-layout__card {
  align-content: center;
  padding: 1.5rem;
}

.auth-layout__field {
  display: grid;
  gap: 0.5rem;
}

.auth-layout__input {
  min-height: 46px;
  padding: 0 0.9rem;
  border: 1px solid var(--panel-border);
  border-radius: 14px;
  background: var(--panel-bg-strong);
  color: var(--text-main);
}

.auth-layout__actions {
  display: flex;
  align-items: center;
  gap: 1rem;
  justify-content: flex-end;
}

.auth-layout__secondary-link {
  color: var(--primary);
  font-weight: 600;
}

@media (max-width: 1100px) {
  .auth-layout {
    grid-template-columns: 1fr;
  }
}
</style>
