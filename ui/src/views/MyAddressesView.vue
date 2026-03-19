<script setup lang="ts">
import PageHeader from '@/components/app/PageHeader.vue'
import ProblemStatePanel from '@/components/shared/ProblemStatePanel.vue'
import { useSessionStore } from '@/stores/session'

const session = useSessionStore()
</script>

<template>
  <ProblemStatePanel
    v-if="!session.currentEmployee"
    code="403"
    title="Address access denied"
    description="Basic self-service access requires a linked employee record."
  />

  <section v-else class="view-stack">
    <PageHeader
      title="My addresses"
      description="Basic self-service address management with ownership-scoped actions only."
    />

    <div class="card-grid">
      <article v-for="address in session.currentEmployee.addresses" :key="address.id" class="section-card address-card">
        <span v-if="address.isPrimary" class="status-pill status-pill--brand">Primary</span>
        <h3>{{ address.addressType }}</h3>
        <p class="text-muted">
          {{ address.line1 }}<span v-if="address.line2">, {{ address.line2 }}</span><br />
          {{ address.city }}, {{ address.state }} {{ address.zipCode }}
        </p>
        <div class="address-card__actions">
          <UButton color="primary" variant="soft">Set primary</UButton>
          <UButton color="error" variant="soft">Delete</UButton>
        </div>
      </article>
    </div>
  </section>
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
  gap: 0.75rem;
  padding: 1rem;
}

.address-card__actions {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
}

@media (max-width: 900px) {
  .card-grid {
    grid-template-columns: 1fr;
  }
}
</style>
