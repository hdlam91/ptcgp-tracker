<script setup lang="ts">
import AppHeader from '@/components/layout/AppHeader.vue'
import ThemeToggle from '@/components/ThemeToggle.vue'
import { useAuth } from '@/composables/useAuth'

const { isAuthenticated } = useAuth()
</script>

<template>
  <div class="min-h-screen bg-background">
    <AppHeader v-if="isAuthenticated" />
    <ThemeToggle v-else class="fixed right-3 top-3 z-10 sm:right-4 sm:top-4" />
    <RouterView />
    <RouterLink
      to="/legal"
      class="fixed bottom-3 left-3 z-10 text-xs text-muted-foreground hover:text-foreground hover:underline sm:bottom-4 sm:left-4"
    >
      Legal
    </RouterLink>
  </div>
</template>
