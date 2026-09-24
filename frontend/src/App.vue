<script setup lang="ts">
import AppHeader from '@/components/layout/AppHeader.vue'
import { useAuth } from '@/composables/useAuth'

const { isAuthenticated } = useAuth()
</script>

<template>
  <div class="min-h-screen bg-background">
    <AppHeader v-if="isAuthenticated" />
    <RouterView />
  </div>
</template>
