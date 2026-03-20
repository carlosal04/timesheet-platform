<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import PageHeader from '@/components/app/PageHeader.vue'
import EmptyState from '@/components/shared/EmptyState.vue'
import ProblemStatePanel from '@/components/shared/ProblemStatePanel.vue'
import { listAuditLogs } from '@/services/api/auditLogs'
import { isApiProblemError, isProblemStatus } from '@/services/api/http'
import { useSessionStore } from '@/stores/session'
import { formatUtcDateTime } from '@/utils/date'
import type { AuditLogItem } from '@/services/api/auditLogs'

const route = useRoute()
const router = useRouter()
const session = useSessionStore()

const actorUserId = ref('')
const actionType = ref('')
const entityType = ref('')
const result = ref('')
const page = ref(1)
const pageSize = ref(12)
const records = ref<AuditLogItem[]>([])
const totalCount = ref(0)
const loading = ref(true)
const problem = ref<null | { code: string; title: string; description: string }>(null)

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

async function loadAuditLogs() {
  loading.value = true
  problem.value = null

  try {
    const response = await listAuditLogs({
      page: page.value,
      pageSize: pageSize.value,
      actorUserId: actorUserId.value.trim() || undefined,
      actionType: actionType.value.trim() || undefined,
      entityType: entityType.value.trim() || undefined,
      result: result.value.trim() || undefined,
    })

    records.value = response.items
    totalCount.value = response.totalCount
    page.value = response.page
    pageSize.value = response.pageSize
  } catch (error) {
    records.value = []
    totalCount.value = 0

    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      await router.replace({ name: 'login', query: { redirect: route.fullPath } })
      return
    }

    if (isApiProblemError(error)) {
      problem.value = {
        code: String(error.status || 500),
        title: error.problem.title ?? 'Audit log unavailable',
        description: error.problem.detail ?? 'The audit log could not be loaded from the EMS API.',
      }
      return
    }

    problem.value = {
      code: '500',
      title: 'Audit log unavailable',
      description: 'The audit log could not be loaded from the EMS API.',
    }
  } finally {
    loading.value = false
  }
}

function resetFilters() {
  actorUserId.value = ''
  actionType.value = ''
  entityType.value = ''
  result.value = ''
  page.value = 1
}

watch([actorUserId, actionType, entityType, result], () => {
  page.value = 1
})

watch([actorUserId, actionType, entityType, result, page, pageSize], () => {
  void loadAuditLogs()
})

watch(
  () => route.fullPath,
  () => {
    void loadAuditLogs()
  },
  { immediate: true },
)
</script>

<template>
  <section class="view-stack">
    <PageHeader
      title="Audit logs"
      description="Dense, filterable audit review surface sourced from the live EMS backend."
    />

    <div class="section-card filters">
      <input v-model="actorUserId" class="filters__input" placeholder="Filter by actor user id" />
      <input v-model="actionType" class="filters__input" placeholder="Filter by action type" />
      <input v-model="entityType" class="filters__input" placeholder="Filter by entity type" />
      <input v-model="result" class="filters__input" placeholder="Filter by result" />
      <button class="filters__button" type="button" @click="resetFilters">Reset</button>
    </div>

    <div v-if="loading" class="section-card">
      <p class="text-muted">Loading audit logs...</p>
    </div>

    <ProblemStatePanel
      v-else-if="problem"
      :code="problem.code"
      :title="problem.title"
      :description="problem.description"
    />

    <EmptyState
      v-else-if="!records.length"
      title="No audit entries match the current filters."
      description="Try a broader actor, action, entity, or result filter."
    />

    <div v-else class="section-card audit-table">
      <table class="table">
        <thead>
          <tr>
            <th>Time</th>
            <th>Action</th>
            <th>Result</th>
            <th>Actor user</th>
            <th>Entity</th>
            <th>Correlation</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="record in records" :key="record.id">
            <td>{{ formatUtcDateTime(record.occurredAtUtc) }}</td>
            <td><span class="status-pill status-pill--primary">{{ record.actionType }}</span></td>
            <td>{{ record.result }}</td>
            <td class="text-muted">{{ record.actorUserId ?? 'System' }}</td>
            <td class="text-muted">
              {{ record.entityType }}<span v-if="record.entityId"> · {{ record.entityId }}</span>
            </td>
            <td class="text-muted">{{ record.correlationId }}</td>
          </tr>
        </tbody>
      </table>

      <div class="audit-table__footer">
        <span class="text-muted">Page {{ page }} of {{ totalPages }}</span>
        <div class="audit-table__pagination">
          <UButton color="neutral" variant="soft" :disabled="page === 1" @click="page -= 1">Previous</UButton>
          <UButton color="neutral" variant="soft" :disabled="page === totalPages" @click="page += 1">
            Next
          </UButton>
        </div>
      </div>
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
  grid-template-columns: repeat(4, minmax(0, 1fr)) auto;
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

.filters__button {
  min-height: 44px;
  padding: 0 1rem;
  border: 1px solid var(--panel-border);
  border-radius: 14px;
  background: transparent;
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

.audit-table__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding-top: 1rem;
}

.audit-table__pagination {
  display: flex;
  gap: 0.75rem;
}

@media (max-width: 1100px) {
  .filters {
    grid-template-columns: 1fr;
  }
}
</style>
