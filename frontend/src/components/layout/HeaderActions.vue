<script setup lang="ts">
import { LogOut, Settings } from '@lucide/vue'
import { useRouter } from 'vue-router'
import ThemeToggle from '@/components/ThemeToggle.vue'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/composables/useAuth'

const { currentUser, isAdmin, logout } = useAuth()
const router = useRouter()

async function onLogout() {
  await logout()
  await router.push('/login')
}
</script>

<template>
  <div class="flex shrink-0 items-center gap-3">
    <span class="hidden text-sm text-muted-foreground sm:inline">{{ currentUser?.displayName }}</span>
    <RouterLink v-if="isAdmin" v-slot="{ isActive, href, navigate }" to="/settings" custom>
      <Button as-child :variant="isActive ? 'default' : 'outline'" size="icon" title="Settings">
        <a :href="href" aria-label="Settings" @click="navigate">
          <Settings class="size-4" />
        </a>
      </Button>
    </RouterLink>
    <ThemeToggle />
    <!-- Icon-only on phones so everything fits; the name stays "Log out". -->
    <Button variant="outline" size="sm" aria-label="Log out" class="max-sm:w-10 max-sm:px-0" @click="onLogout">
      <LogOut class="size-4 sm:hidden" />
      <span class="max-sm:hidden">Log out</span>
    </Button>
  </div>
</template>
