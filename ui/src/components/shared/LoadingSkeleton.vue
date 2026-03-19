<template>
  <div class="skeleton section-card">
    <div class="skeleton__line skeleton__line--lg"></div>
    <div class="skeleton__line"></div>
    <div class="skeleton__line"></div>
    <div class="skeleton__line skeleton__line--sm"></div>
  </div>
</template>

<style scoped>
.skeleton {
  display: grid;
  gap: 0.75rem;
  padding: 1.4rem;
}

.skeleton__line {
  height: 0.95rem;
  border-radius: 999px;
  background:
    linear-gradient(90deg, var(--panel-border) 20%, color-mix(in srgb, var(--panel-bg) 60%, white) 50%, var(--panel-border) 80%);
  background-size: 200% 100%;
  animation: shimmer 1.4s infinite linear;
}

.skeleton__line--lg {
  width: 70%;
  height: 1.1rem;
}

.skeleton__line--sm {
  width: 35%;
}

@keyframes shimmer {
  from { background-position: 200% 0; }
  to { background-position: -200% 0; }
}
</style>
