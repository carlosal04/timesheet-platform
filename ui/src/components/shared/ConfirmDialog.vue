<script setup lang="ts">
defineProps<{
  title: string
  description: string
  modelValue: boolean
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  confirm: []
}>()
</script>

<template>
  <div v-if="modelValue" class="confirm">
    <div class="confirm__backdrop" @click="emit('update:modelValue', false)"></div>
    <div class="confirm__panel section-card">
      <h3>{{ title }}</h3>
      <p class="text-muted">{{ description }}</p>
      <div class="confirm__actions">
        <UButton color="neutral" variant="soft" @click="emit('update:modelValue', false)">Cancel</UButton>
        <UButton color="error" @click="emit('confirm')">Confirm</UButton>
      </div>
    </div>
  </div>
</template>

<style scoped>
.confirm {
  position: fixed;
  inset: 0;
  z-index: 50;
}

.confirm__backdrop {
  position: absolute;
  inset: 0;
  background: rgba(2, 6, 23, 0.5);
}

.confirm__panel {
  position: relative;
  width: min(420px, calc(100% - 32px));
  margin: 12vh auto 0;
  padding: 1.5rem;
}

.confirm__actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  margin-top: 1rem;
}
</style>
