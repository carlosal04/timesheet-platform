<script setup lang="ts">
defineProps<{
  code: string
  title: string
  description: string
}>()
</script>

<template>
  <div class="problem section-card">
    <span class="status-pill status-pill--error">{{ code }}</span>
    <h3>{{ title }}</h3>
    <p class="text-muted">{{ description }}</p>
  </div>
</template>

<style scoped>
.problem {
  display: grid;
  gap: 0.7rem;
  padding: 1.5rem;
}
</style>
