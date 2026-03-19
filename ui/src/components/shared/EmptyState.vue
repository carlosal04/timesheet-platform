<script setup lang="ts">
defineProps<{
  title: string
  description: string
}>()
</script>

<template>
  <div class="empty-state section-card">
    <h3>{{ title }}</h3>
    <p class="text-muted">{{ description }}</p>
  </div>
</template>

<style scoped>
.empty-state {
  display: grid;
  gap: 0.5rem;
  padding: 1.5rem;
  text-align: center;
}
</style>
