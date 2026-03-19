<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useSessionStore } from '@/stores/session'

const router = useRouter()
const route = useRoute()
const session = useSessionStore()
const email = ref('admin@example.com')
const password = ref('')

function useLocalBootstrapExample() {
  email.value = 'admin@example.com'
  password.value = ''
  session.clearProblem()
}

async function handleLogin() {
  const success = await session.login(email.value, password.value)

  if (!success) {
    return
  }

  const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/employees'
  await router.push(redirect)
}
</script>

<template>
  <div class="app-page">
    <section class="page-card login-layout">
      <div class="login-layout__hero">
        <div>
          <span class="status-pill status-pill--brand">EMS frontend integration</span>
          <h1>Employment Management System</h1>
          <p class="text-muted">
            Sign in with the EMS bootstrap admin credentials configured for your local runtime. The shell,
            theme system, session timer, and route posture now run against the real backend auth flow.
          </p>
        </div>

        <div class="login-layout__demo-accounts">
          <button class="login-layout__demo-button" type="button" @click="useLocalBootstrapExample()">
            Prefill local example email
          </button>
          <p class="text-muted">
            Default local example: <strong>admin@example.com</strong>. The password comes from your EMS bootstrap
            admin setup, typically via <code>EMS/.env</code>.
          </p>
        </div>
      </div>

      <form class="section-card login-layout__card" @submit.prevent="handleLogin">
        <div>
          <span class="status-pill status-pill--primary">Sign in</span>
          <h2>Access the EMS workspace</h2>
          <p class="text-muted">
            System theme is used first, then your override is remembered locally. Successful sign-in bootstraps the
            authenticated session and anti-forgery state from the API.
          </p>
        </div>

        <div class="login-layout__field">
          <label for="email">Email</label>
          <input
            id="email"
            v-model="email"
            class="login-layout__input"
            type="email"
            autocomplete="username"
            placeholder="admin@example.com"
          />
        </div>

        <div class="login-layout__field">
          <label for="password">Password</label>
          <input
            id="password"
            v-model="password"
            class="login-layout__input"
            type="password"
            autocomplete="current-password"
            placeholder="Enter your local EMS password"
          />
        </div>

        <div v-if="session.loginProblem" class="login-layout__problem">
          <strong>{{ session.loginProblem.title }}</strong>
          <p>{{ session.loginProblem.detail }}</p>
        </div>

        <div class="login-layout__actions">
          <UButton type="submit" :loading="session.isBusy || session.isHydrating">Sign in</UButton>
        </div>
      </form>
    </section>
  </div>
</template>

<style scoped>
.login-layout {
  display: grid;
  grid-template-columns: 1.1fr 420px;
  gap: 1.25rem;
  padding: 1.2rem;
}

.login-layout__hero {
  display: grid;
  gap: 1.25rem;
  padding: 1.7rem;
  border-radius: 22px;
  background: linear-gradient(135deg, var(--surface-dark), var(--surface-dark-muted));
  color: var(--surface-dark-text);
}

.login-layout__hero h1 {
  margin: 1rem 0 0;
  font-size: clamp(2.8rem, 5vw, 4.6rem);
  line-height: 0.95;
  letter-spacing: -0.05em;
}

.login-layout__demo-accounts {
  display: grid;
  gap: 0.75rem;
}

.login-layout__demo-button {
  min-height: 44px;
  padding: 0 1rem;
  border: 1px solid rgba(226, 232, 240, 0.12);
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.04);
  color: var(--surface-dark-text);
  text-align: left;
}

.login-layout__card {
  display: grid;
  gap: 1rem;
  align-content: center;
  padding: 1.5rem;
}

.login-layout__field {
  display: grid;
  gap: 0.5rem;
}

.login-layout__input {
  min-height: 46px;
  padding: 0 0.9rem;
  border: 1px solid var(--panel-border);
  border-radius: 14px;
  background: var(--panel-bg-strong);
  color: var(--text-main);
}

.login-layout__problem {
  padding: 1rem;
  border: 1px solid color-mix(in srgb, var(--error) 30%, var(--panel-border));
  border-radius: 16px;
  background: color-mix(in srgb, var(--error-soft) 82%, var(--panel-bg));
  color: var(--error);
}

.login-layout__problem p {
  margin: 0.35rem 0 0;
}

.login-layout__actions {
  display: flex;
  justify-content: flex-end;
}

@media (max-width: 1100px) {
  .login-layout {
    grid-template-columns: 1fr;
  }
}
</style>
