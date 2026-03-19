<template>
  <div class="app-page">
    <ProblemStatePanel
      code="403"
      title="Access denied"
      description="This route is intentionally protected for a different role or ownership posture."
    />
  </div>
</template>

<script setup lang="ts">
import ProblemStatePanel from '@/components/shared/ProblemStatePanel.vue'
</script>
