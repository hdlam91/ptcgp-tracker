import { computed, ref } from 'vue'

type Theme = 'light' | 'dark'

const STORAGE_KEY = 'ptcgp-theme'

// The initial value mirrors whatever index.html's inline script already applied
// to <html> before this module loads, so there's a single source of truth and
// no flash of the wrong theme while the JS bundle boots.
const theme = ref<Theme>(document.documentElement.classList.contains('dark') ? 'dark' : 'light')

function applyTheme(value: Theme) {
  document.documentElement.classList.toggle('dark', value === 'dark')
  try {
    localStorage.setItem(STORAGE_KEY, value)
  }
  catch {
    // Private browsing / storage disabled — theme just won't persist across reloads.
  }
}

function setTheme(value: Theme) {
  theme.value = value
  applyTheme(value)
}

function toggleTheme() {
  setTheme(theme.value === 'dark' ? 'light' : 'dark')
}

export function useTheme() {
  return {
    theme: computed(() => theme.value),
    isDark: computed(() => theme.value === 'dark'),
    setTheme,
    toggleTheme,
  }
}
