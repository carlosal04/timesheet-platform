<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import PageHeader from '@/components/app/PageHeader.vue'
import ProblemStatePanel from '@/components/shared/ProblemStatePanel.vue'
import { findEmployee } from '@/mocks/ems'

const route = useRoute()
const employee = computed(() => findEmployee(String(route.params.id)))
</script>

<template>
  <ProblemStatePanel
    v-if="!employee"
    code="404"
    title="Employee not found"
    description="The requested employee is missing or hidden in the current posture."
  />

  <section v-else class="view-stack">
    <PageHeader
      :title="`${employee.firstName} ${employee.lastName}`"
      description="Employee profile, active addresses, and the admin path into edit and address management."
    >
      <template #actions>
        <RouterLink to="/employees">
          <UButton color="neutral" variant="soft">Back to list</UButton>
        </RouterLink>
        <RouterLink :to="`/employees/${employee.id}/edit`">
          <UButton>Edit employee</UButton>
        </RouterLink>
      </template>
    </PageHeader>

    <div class="detail-grid">
      <div class="section-card panel">
        <h3>Identity and employment</h3>
        <dl class="definition-list">
          <div><dt>Email</dt><dd>{{ employee.email }}</dd></div>
          <div><dt>Phone</dt><dd>{{ employee.phone }}</dd></div>
          <div><dt>Date of birth</dt><dd>{{ employee.dateOfBirth }}</dd></div>
          <div><dt>Hire date</dt><dd>{{ employee.hireDate }}</dd></div>
          <div><dt>Status</dt><dd>{{ employee.status }}</dd></div>
        </dl>
      </div>

      <div class="section-card panel">
        <div class="panel__header">
          <h3>Addresses</h3>
          <RouterLink :to="`/employees/${employee.id}/addresses`">
            <UButton color="neutral" variant="soft">Manage addresses</UButton>
          </RouterLink>
        </div>
        <div class="address-grid">
          <article v-for="address in employee.addresses" :key="address.id" class="address-card">
            <span v-if="address.isPrimary" class="status-pill status-pill--brand">Primary</span>
            <h4>{{ address.addressType }}</h4>
            <p class="text-muted">
              {{ address.line1 }}<span v-if="address.line2">, {{ address.line2 }}</span><br />
              {{ address.city }}, {{ address.state }} {{ address.zipCode }}
            </p>
          </article>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.view-stack,
.detail-grid,
.address-grid {
  display: grid;
  gap: 1rem;
}

.detail-grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.panel {
  padding: 1.2rem;
}

.panel__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.definition-list {
  display: grid;
  gap: 0.75rem;
  margin-top: 1rem;
}

.definition-list div {
  display: grid;
  gap: 0.2rem;
}

dt {
  font-size: 0.8rem;
  font-weight: 700;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.address-grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
  margin-top: 1rem;
}

.address-card {
  padding: 1rem;
  border: 1px solid var(--panel-border);
  border-radius: 16px;
  background: var(--panel-bg-strong);
}

.address-card h4 {
  margin: 0.65rem 0 0;
}

@media (max-width: 900px) {
  .detail-grid,
  .address-grid {
    grid-template-columns: 1fr;
  }
}
</style>
