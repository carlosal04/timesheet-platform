import { computed, watch } from 'vue'
import { defineStore } from 'pinia'
import { usePreferredDark, useStorage } from '@vueuse/core'

type ThemeChoice = 'light' | 'dark' | null

export const useThemeStore = defineStore('theme', () => {
  const storedChoice = useStorage<ThemeChoice>('ems-theme', null)
  const prefersDark = usePreferredDark()
  const resolvedTheme = computed<'light' | 'dark'>(() => {
    if (storedChoice.value) {
      return storedChoice.value
    }

    return prefersDark.value ? 'dark' : 'light'
  })
  const isDark = computed(() => resolvedTheme.value === 'dark')
  const usingSystemPreference = computed(() => storedChoice.value === null)

  let stopSync: (() => void) | null = null

  function applyTheme(theme: 'light' | 'dark') {
    const root = document.documentElement
    root.dataset.theme = theme
    root.classList.toggle('theme-dark', theme === 'dark')
    root.classList.toggle('theme-light', theme === 'light')
  }

  function initialize() {
    if (stopSync) {
      return
    }

    stopSync = watch(resolvedTheme, applyTheme, { immediate: true })
  }

  function toggleTheme() {
    storedChoice.value = resolvedTheme.value === 'dark' ? 'light' : 'dark'
  }

  return {
    isDark,
    resolvedTheme,
    usingSystemPreference,
    initialize,
    toggleTheme,
  }
})
