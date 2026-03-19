<script setup lang="ts">
import AppSidebar from '@/components/app/AppSidebar.vue'
import AppTopbar from '@/components/app/AppTopbar.vue'
import SessionWarningBanner from '@/components/app/SessionWarningBanner.vue'
</script>

<template>
  <div class="app-page shell-layout">
    <aside class="shell-layout__sidebar">
      <AppSidebar />
    </aside>

    <section class="shell-layout__main">
      <AppTopbar />
      <SessionWarningBanner />
      <div class="shell-layout__content">
        <slot />
      </div>
    </section>
  </div>
</template>

<style scoped>
.shell-layout {
  display: grid;
  grid-template-columns: 260px minmax(0, 1fr);
  gap: 1.1rem;
}

.shell-layout__main {
  display: grid;
  gap: 1rem;
}

.shell-layout__content {
  display: grid;
  gap: 1rem;
}

@media (max-width: 1100px) {
  .shell-layout {
    grid-template-columns: 1fr;
  }
}
</style>
