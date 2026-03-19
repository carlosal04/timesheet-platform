<script setup lang="ts">
import { reactive, ref } from 'vue'
import PageHeader from '@/components/app/PageHeader.vue'
import ValidationSummary from '@/components/shared/ValidationSummary.vue'
import { useSessionStore } from '@/stores/session'

const session = useSessionStore()
const selectedRoles = reactive(
  Object.fromEntries(session.mockUsers.map((user) => [user.id, user.roleCode])) as Record<string, 'Admin' | 'Basic'>,
)
const messages = ref<string[]>([])

function applyRole(userId: string) {
  messages.value = []
  const currentAdmins = Object.values(selectedRoles).filter((role) => role === 'Admin').length
  const targetRole = selectedRoles[userId]
  const currentRole = session.mockUsers.find((user) => user.id === userId)?.roleCode

  if (currentRole === 'Admin' && targetRole !== 'Admin' && currentAdmins <= 1) {
    messages.value = ['This change would leave the system with zero active Admin users.']
    return
  }

  messages.value = ['Mock role assignment accepted. In the real integration this will revoke the target user session immediately.']
}
</script>

<template>
  <section class="view-stack">
    <PageHeader
      title="Roles"
      description="Admin-only role management using the approved role codes and zero-admin protection rule."
    />

    <ValidationSummary :messages="messages" />

    <div class="section-card roles-card">
      <table class="table">
        <thead>
          <tr>
            <th>User</th>
            <th>Email</th>
            <th>Role</th>
            <th>Action</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="user in session.mockUsers" :key="user.id">
            <td>{{ user.name }}</td>
            <td>{{ user.email }}</td>
            <td>
              <select v-model="selectedRoles[user.id]" class="roles-card__select">
                <option v-for="role in session.mockRoles" :key="role.id" :value="role.code">
                  {{ role.name }}
                </option>
              </select>
            </td>
            <td>
              <UButton color="primary" variant="soft" @click="applyRole(user.id)">Apply role</UButton>
            </td>
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

.roles-card {
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
}

.roles-card__select {
  min-height: 42px;
  padding: 0 0.85rem;
  border: 1px solid var(--panel-border);
  border-radius: 12px;
  background: var(--panel-bg-strong);
  color: var(--text-main);
}
</style>
