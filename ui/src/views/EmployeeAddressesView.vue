<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'
import ConfirmDialog from '@/components/shared/ConfirmDialog.vue'
import PageHeader from '@/components/app/PageHeader.vue'
import ProblemStatePanel from '@/components/shared/ProblemStatePanel.vue'
import { findEmployee } from '@/mocks/ems'

const route = useRoute()
const employee = computed(() => findEmployee(String(route.params.id)))
const addressPendingDelete = ref<string | null>(null)
</script>

<template>
  <ProblemStatePanel
    v-if="!employee"
    code="404"
    title="Employee not found"
    description="Address management needs a visible employee record."
  />

  <section v-else class="view-stack">
    <PageHeader
      :title="`${employee.firstName} ${employee.lastName} addresses`"
      description="Admin-only address management with a clear primary-address rule and delete confirmation posture."
    >
      <template #actions>
        <UButton>Add address</UButton>
      </template>
    </PageHeader>

    <div class="card-grid">
      <article v-for="address in employee.addresses" :key="address.id" class="section-card address-card">
        <div class="address-card__header">
          <span v-if="address.isPrimary" class="status-pill status-pill--brand">Primary</span>
          <span class="text-muted">{{ address.addressType }}</span>
        </div>
        <p class="text-muted">
          {{ address.line1 }}<span v-if="address.line2">, {{ address.line2 }}</span><br />
          {{ address.city }}, {{ address.state }} {{ address.zipCode }}
        </p>
        <div class="address-card__actions">
          <UButton color="neutral" variant="soft">Edit</UButton>
          <UButton color="primary" variant="soft">Set primary</UButton>
          <UButton color="error" variant="soft" @click="addressPendingDelete = address.id">Delete</UButton>
        </div>
      </article>
    </div>

    <ConfirmDialog
      :model-value="Boolean(addressPendingDelete)"
      title="Delete address?"
      description="This mock action demonstrates the confirmation flow and the future 409/error posture without mutating shared data."
      @update:model-value="addressPendingDelete = null"
      @confirm="addressPendingDelete = null"
    />
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
