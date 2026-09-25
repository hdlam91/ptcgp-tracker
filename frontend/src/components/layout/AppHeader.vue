<script setup lang="ts">
import { useEventListener, useResizeObserver } from '@vueuse/core'
import { LogOut, Menu, Moon, Settings, Sun, X } from '@lucide/vue'
import { nextTick, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppNav from '@/components/layout/AppNav.vue'
import HeaderActions from '@/components/layout/HeaderActions.vue'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/composables/useAuth'
import { useTheme } from '@/composables/useTheme'

const { currentUser, isAdmin, logout } = useAuth()
const { isDark, toggleTheme } = useTheme()
const router = useRouter()
const route = useRoute()

async function onLogout() {
  open.value = false
  await logout()
  await router.push('/login')
}

// The menu sits inline while the whole thing fits, and turns into a hamburger dropdown only when it
// doesn't. "Fits" is measured rather than guessed from a screen width, because what it needs
// changes with who's logged in: admins get an extra icon, and display names vary in length.
const rowEl = ref<HTMLElement | null>(null)
const leftEl = ref<HTMLElement | null>(null)
const brandEl = ref<HTMLElement | null>(null)
const measureActionsEl = ref<HTMLElement | null>(null)
const measureEl = ref<HTMLElement | null>(null)
const collapsed = ref(false)

function px(value: string) {
  return Number.parseFloat(value) || 0
}

function updateCollapsed() {
  const row = rowEl.value
  const left = leftEl.value
  const brand = brandEl.value
  const actions = measureActionsEl.value
  const measure = measureEl.value
  if (!row || !left || !brand || !actions || !measure) return

  const rowStyle = getComputedStyle(row)
  const available = row.clientWidth - px(rowStyle.paddingLeft) - px(rowStyle.paddingRight)
  const brandWidth = brand.offsetWidth
  const needed
    = brandWidth
      + (brandWidth > 0 ? px(getComputedStyle(left).columnGap) : 0)
      + measure.scrollWidth
      + px(rowStyle.columnGap)
      + actions.offsetWidth

  collapsed.value = needed > available
}

// Everything the calculation depends on is always rendered (including an invisible full-width copy
// of the menu and of the right-hand actions), so the answer never depends on which mode is showing and can't flip back and forth.
useResizeObserver([rowEl, brandEl, measureActionsEl, measureEl], updateCollapsed)
onMounted(() => nextTick(updateCollapsed))

const open = ref(false)

watch(collapsed, (isCollapsed) => {
  if (!isCollapsed) open.value = false
})
watch(() => route.fullPath, () => { open.value = false })
// Any press that isn't on the dropdown itself or on the button that toggles it closes the menu.
useEventListener(document, 'pointerdown', (event: PointerEvent) => {
  if (!open.value) return
  if ((event.target as Element | null)?.closest('#app-menu, [aria-controls="app-menu"]')) return
  open.value = false
})
useEventListener(document, 'keydown', (event: KeyboardEvent) => {
  if (event.key === 'Escape') open.value = false
})
</script>

<template>
  <header ref="rowEl" class="relative flex items-center justify-between gap-2 border-b px-3 py-3 sm:gap-4 sm:px-6">
    <div ref="leftEl" class="flex min-w-0 items-center gap-3 sm:gap-6">
      <!-- Never shrinks: its width feeds the "does the menu fit?" calculation, so it must be its natural width. -->
      <div ref="brandEl" class="flex shrink-0 items-center">
        <span class="whitespace-nowrap text-base font-semibold sm:hidden">PTCGP</span>
        <span class="hidden whitespace-nowrap text-base font-semibold sm:inline sm:text-lg">PTCGP Tracker</span>
      </div>
      <AppNav v-if="!collapsed" />
    </div>

    <HeaderActions v-if="!collapsed" />
    <Button
      v-else
      variant="outline"
      size="icon"
      class="shrink-0"
      :aria-label="open ? 'Close menu' : 'Open menu'"
      :aria-expanded="open"
      aria-controls="app-menu"
      @click="open = !open"
    >
      <X v-if="open" class="size-4" />
      <Menu v-else class="size-4" />
    </Button>

    <!-- Invisible, out of the layout and out of reach of screen readers and the keyboard: only here so we
         can measure how wide the full menu and the actions want to be. -->
    <div class="pointer-events-none invisible absolute left-0 top-0 -z-10 flex w-max" aria-hidden="true" inert>
      <div ref="measureEl"><AppNav /></div>
      <div ref="measureActionsEl"><HeaderActions /></div>
    </div>

    <!-- The hamburger dropdown, sliding out under the header. -->
    <div
      v-if="collapsed && open"
      id="app-menu"
      class="absolute inset-x-0 top-full z-30 border-b bg-background shadow-md"
    >
      <AppNav vertical />
      <div class="border-t py-1">
        <p v-if="currentUser?.displayName" class="px-4 pb-1 pt-2 text-sm text-muted-foreground">
          {{ currentUser.displayName }}
        </p>
        <RouterLink
          v-if="isAdmin"
          to="/settings"
          class="flex items-center gap-3 px-4 py-3 text-base hover:bg-accent"
          active-class="font-medium text-foreground"
        >
          <Settings class="size-4" />
          Settings
        </RouterLink>
        <button type="button" class="flex w-full items-center gap-3 px-4 py-3 text-left text-base hover:bg-accent" @click="toggleTheme">
          <Sun v-if="isDark" class="size-4" />
          <Moon v-else class="size-4" />
          {{ isDark ? 'Switch to light mode' : 'Switch to dark mode' }}
        </button>
        <button type="button" class="flex w-full items-center gap-3 px-4 py-3 text-left text-base hover:bg-accent" @click="onLogout">
          <LogOut class="size-4" />
          Log out
        </button>
      </div>
    </div>
  </header>
</template>
