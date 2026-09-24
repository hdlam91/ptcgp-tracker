<script setup lang="ts">
import { useRouter } from 'vue-router'
import AppNav from '@/components/layout/AppNav.vue'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/composables/useAuth'

const { currentUser, logout } = useAuth()
const router = useRouter()

async function onLogout() {
  await logout()
  await router.push('/login')
}
</script>

<template>
  <header class="flex items-center justify-between gap-2 border-b px-3 py-3 sm:gap-4 sm:px-6">
    <div class="flex min-w-0 items-center gap-3 sm:gap-6">
      <span class="whitespace-nowrap text-base font-semibold sm:text-lg">PTCGP Tracker</span>
      <AppNav />
    </div>
    <div class="flex shrink-0 items-center gap-3">
      <span class="hidden text-sm text-muted-foreground sm:inline">{{ currentUser?.displayName }}</span>
      <Button variant="outline" size="sm" @click="onLogout">
        Log out
      </Button>
    </div>
  </header>
</template>
