<script setup lang="ts">
import { useSessionStore } from '@/stores/session'

const session = useSessionStore()
</script>

<template>
  <div v-if="session.isInWarningWindow" class="warning-banner">
    <div>
      <strong>Session nearing idle timeout.</strong>
      <p class="text-muted">
        Your current session expires in {{ session.remainingLabel }}. Renewing now extends it from the current
        time, matching the approved frontend-driven renew model.
      </p>
    </div>
    <UButton color="warning" :loading="session.isBusy" @click="session.renewSession()">Stay signed in</UButton>
  </div>
</template>

<style scoped>
.warning-banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1rem 1.2rem;
  border-radius: 18px;
  border: 1px solid color-mix(in srgb, var(--warning) 28%, var(--panel-border));
  background: color-mix(in srgb, var(--warning-soft) 82%, var(--panel-bg));
}

p {
  margin: 0.35rem 0 0;
}
</style>
