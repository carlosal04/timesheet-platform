<template>
  <div class="app-page">
    <ProblemStatePanel
      code="404"
      title="Page not found"
      description="The requested page does not exist in the current EMS UI route map."
    />
  </div>
</template>

<script setup lang="ts">
import ProblemStatePanel from '@/components/shared/ProblemStatePanel.vue'
</script>
