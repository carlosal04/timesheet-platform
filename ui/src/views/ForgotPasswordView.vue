<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { RouterLink } from 'vue-router'
import ProblemStatePanel from '@/components/shared/ProblemStatePanel.vue'
import ValidationSummary from '@/components/shared/ValidationSummary.vue'
import { forgotPassword } from '@/services/api/auth'
import { isApiProblemError } from '@/services/api/http'

const loading = ref(false)
const submitted = ref(false)
const success = ref(false)
const validationMessages = ref<string[]>([])
const formProblem = ref<null | { code: string; title: string; description: string }>(null)
const form = reactive({
  email: '',
})

const localValidationMessages = computed(() => {
  const messages: string[] = []

  if (!form.email.includes('@')) {
    messages.push('Enter the email address used for your EMS account.')
  }

  return messages
})

async function submitForm() {
  submitted.value = true
  validationMessages.value = localValidationMessages.value
  formProblem.value = null

  if (validationMessages.value.length) {
    return
  }

  loading.value = true

  try {
    await forgotPassword(form.email.trim())
    success.value = true
  } catch (error) {
    if (isApiProblemError(error)) {
      formProblem.value = {
        code: String(error.status || 400),
        title: error.problem.title ?? 'Unable to start password reset',
        description: error.problem.detail ?? 'The password reset request could not be started.',
      }
      return
    }

    formProblem.value = {
      code: '500',
      title: 'Unable to start password reset',
      description: 'The password reset request could not be started.',
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
        <span class="status-pill status-pill--brand">Password recovery</span>
        <div>
          <h1>Reset your password</h1>
          <p class="text-muted">
            Enter your email address and EMS will send a password reset link if your account is eligible for
            self-service recovery.
          </p>
        </div>
      </div>

      <form class="section-card auth-layout__card" @submit.prevent="submitForm">
        <div v-if="success" class="auth-layout__success">
          <strong>Check your email</strong>
          <p>
            If the account is eligible for self-service reset, a reset email has been sent. The response is the
            same even when the email is unknown.
          </p>
        </div>

        <ProblemStatePanel
          v-if="formProblem"
          :code="formProblem.code"
          :title="formProblem.title"
          :description="formProblem.description"
        />

        <ValidationSummary :messages="submitted ? validationMessages : []" />

        <div class="auth-layout__field">
          <label for="reset-email">Email</label>
          <input
            id="reset-email"
            v-model="form.email"
            class="auth-layout__input"
            type="email"
            autocomplete="email"
            placeholder="name@company.com"
          />
        </div>

        <div class="auth-layout__actions">
          <RouterLink class="auth-layout__secondary-link" :to="{ name: 'login' }">
            Back to sign in
          </RouterLink>
          <UButton type="submit" :loading="loading">Send reset link</UButton>
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

.auth-layout__success {
  padding: 1rem 1.1rem;
  border-radius: 16px;
  border: 1px solid color-mix(in srgb, var(--success) 32%, var(--panel-border));
  background: color-mix(in srgb, var(--success-soft) 82%, var(--panel-bg));
  color: var(--success);
}

.auth-layout__success p {
  margin: 0.35rem 0 0;
}

@media (max-width: 1100px) {
  .auth-layout {
    grid-template-columns: 1fr;
  }
}
</style>
