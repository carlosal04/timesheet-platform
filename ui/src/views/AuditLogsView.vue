<script setup lang="ts">
import { computed, ref } from 'vue'
import PageHeader from '@/components/app/PageHeader.vue'
import EmptyState from '@/components/shared/EmptyState.vue'
import { useSessionStore } from '@/stores/session'
import { formatUtcDateTime } from '@/utils/date'

const session = useSessionStore()
const actionFilter = ref('')
const actorFilter = ref('')

const filteredRecords = computed(() =>
  session.auditRecords.filter((record) => {
    const matchesAction = !actionFilter.value || record.actionType.toLowerCase().includes(actionFilter.value.toLowerCase())
    const matchesActor = !actorFilter.value || record.actorEmail.toLowerCase().includes(actorFilter.value.toLowerCase())

    return matchesAction && matchesActor
  }),
)
</script>

<template>
  <section class="view-stack">
    <PageHeader
      title="Audit logs"
      description="Dense, filterable review surface for the audit taxonomy already implemented on the backend."
    />

    <div class="section-card filters">
      <input v-model="actorFilter" class="filters__input" placeholder="Filter by actor email" />
      <input v-model="actionFilter" class="filters__input" placeholder="Filter by action type" />
    </div>

    <EmptyState
      v-if="!filteredRecords.length"
      title="No audit entries match the current filters."
      description="Try a broader actor or action filter."
    />

    <div v-else class="section-card audit-table">
      <table class="table">
        <thead>
          <tr>
            <th>Time</th>
            <th>Action</th>
            <th>Actor</th>
            <th>Entity</th>
            <th>Summary</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="record in filteredRecords" :key="record.id">
            <td>{{ formatUtcDateTime(record.occurredAtUtc) }}</td>
            <td><span class="status-pill status-pill--primary">{{ record.actionType }}</span></td>
            <td>{{ record.actorEmail }}</td>
            <td>{{ record.entityType }}</td>
            <td class="text-muted">{{ record.summary }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>

<style scoped>
.view-stack {
  display: grid;
  gap: 1rem;
}

.filters {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.75rem;
  padding: 1rem;
}

.filters__input {
  min-height: 44px;
  padding: 0 0.85rem;
  border: 1px solid var(--panel-border);
  border-radius: 14px;
  background: var(--panel-bg-strong);
  color: var(--text-main);
}

.audit-table {
  padding: 1rem;
}

.table {
  width: 100%;
  border-collapse: collapse;
}

.table th,
.table td {
  text-align: left;
  padding: 0.9rem 0;
  border-bottom: 1px solid var(--panel-border);
  vertical-align: top;
}

@media (max-width: 900px) {
  .filters {
    grid-template-columns: 1fr;
  }
}
</style>
