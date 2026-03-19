<script setup lang="ts">
defineProps<{
  messages: string[]
}>()
</script>

<template>
  <div v-if="messages.length" class="summary">
    <strong>Please fix the following:</strong>
    <ul>
      <li v-for="message in messages" :key="message">{{ message }}</li>
    </ul>
  </div>
</template>

<style scoped>
.summary {
  padding: 1rem 1.1rem;
  border-radius: 16px;
  border: 1px solid color-mix(in srgb, var(--error) 32%, var(--panel-border));
  background: color-mix(in srgb, var(--error-soft) 82%, var(--panel-bg));
  color: var(--error);
}

ul {
  margin: 0.5rem 0 0;
  padding-left: 1rem;
}
</style>
