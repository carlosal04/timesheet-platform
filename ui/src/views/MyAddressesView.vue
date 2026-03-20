<script setup lang="ts">
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ConfirmDialog from '@/components/shared/ConfirmDialog.vue'
import PageHeader from '@/components/app/PageHeader.vue'
import ProblemStatePanel from '@/components/shared/ProblemStatePanel.vue'
import { deleteOwnAddress, listOwnAddresses, setOwnAddressPrimary } from '@/services/api/addresses'
import { isApiProblemError, isProblemStatus } from '@/services/api/http'
import { useSessionStore } from '@/stores/session'
import type { EmployeeAddress } from '@/types/ems'

const route = useRoute()
const router = useRouter()
const session = useSessionStore()

const addresses = ref<EmployeeAddress[]>([])
const loading = ref(true)
const actionBusyId = ref<string | null>(null)
const addressPendingDelete = ref<string | null>(null)
const problem = ref<null | { code: string; title: string; description: string }>(null)

async function loadAddresses() {
  loading.value = true
  problem.value = null

  try {
    addresses.value = await listOwnAddresses()
  } catch (error) {
    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      await router.replace({ name: 'login', query: { redirect: route.fullPath } })
      return
    }

    if (isApiProblemError(error)) {
      problem.value = {
        code: String(error.status || 403),
        title: error.problem.title ?? 'Address access denied',
        description: error.problem.detail ?? 'Basic self-service access requires a linked employee record.',
      }
      return
    }

    problem.value = {
      code: '403',
      title: 'Address access denied',
      description: 'Basic self-service access requires a linked employee record.',
    }
  } finally {
    loading.value = false
  }
}

async function handleSetPrimary(addressId: string) {
  actionBusyId.value = addressId
  problem.value = null

  try {
    await setOwnAddressPrimary(addressId)
    await loadAddresses()
  } catch (error) {
    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      await router.replace({ name: 'login', query: { redirect: route.fullPath } })
      return
    }

    if (isApiProblemError(error)) {
      problem.value = {
        code: String(error.status || 409),
        title: error.problem.title ?? 'Unable to change primary address',
        description: error.problem.detail ?? 'The selected address could not be promoted to primary.',
      }
      return
    }

    problem.value = {
      code: '500',
      title: 'Unable to change primary address',
      description: 'The selected address could not be promoted to primary.',
    }
  } finally {
    actionBusyId.value = null
  }
}

async function confirmDelete() {
  if (!addressPendingDelete.value) {
    return
  }

  actionBusyId.value = addressPendingDelete.value
  problem.value = null

  try {
    await deleteOwnAddress(addressPendingDelete.value)
    await loadAddresses()
  } catch (error) {
    if (isProblemStatus(error, 401)) {
      session.handleUnauthorized()
      await router.replace({ name: 'login', query: { redirect: route.fullPath } })
      return
    }

    if (isApiProblemError(error)) {
      problem.value = {
        code: String(error.status || 409),
        title: error.problem.title ?? 'Unable to delete address',
        description: error.problem.detail ?? 'The selected address could not be deleted.',
      }
      return
    }

    problem.value = {
      code: '500',
      title: 'Unable to delete address',
      description: 'The selected address could not be deleted.',
    }
  } finally {
    addressPendingDelete.value = null
    actionBusyId.value = null
  }
}

watch(
  () => route.fullPath,
  () => {
    void loadAddresses()
  },
  { immediate: true },
)
</script>

<template>
  <div v-if="loading" class="section-card">
    <p class="text-muted">Loading your addresses...</p>
  </div>

  <ProblemStatePanel
    v-else-if="problem"
    :code="problem.code"
    :title="problem.title"
    :description="problem.description"
  />

  <section v-else class="view-stack">
    <PageHeader
      title="My addresses"
      description="Basic self-service address management with live ownership-scoped actions."
    />

    <div class="card-grid">
      <article v-for="address in addresses" :key="address.id" class="section-card address-card">
        <span v-if="address.isPrimary" class="status-pill status-pill--brand">Primary</span>
        <h3>{{ address.addressType }}</h3>
        <p class="text-muted">
          {{ address.line1 }}<span v-if="address.line2">, {{ address.line2 }}</span><br />
          {{ address.city }}, {{ address.state }} {{ address.zipCode }}
        </p>
        <div class="address-card__actions">
          <UButton
            color="primary"
            variant="soft"
            :disabled="address.isPrimary"
            :loading="actionBusyId === address.id"
            @click="handleSetPrimary(address.id)"
          >
            Set primary
          </UButton>
          <UButton
            color="error"
            variant="soft"
            :loading="actionBusyId === address.id"
            @click="addressPendingDelete = address.id"
          >
            Delete
          </UButton>
        </div>
      </article>
    </div>

    <ConfirmDialog
      :model-value="Boolean(addressPendingDelete)"
      title="Delete address?"
      description="This performs the approved self-service soft-delete flow for the selected address."
      @update:model-value="addressPendingDelete = null"
      @confirm="confirmDelete"
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
