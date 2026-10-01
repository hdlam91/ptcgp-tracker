<script setup lang="ts">
import { LogOut, Settings, UserCog } from '@lucide/vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import ThemeToggle from '@/components/ThemeToggle.vue'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/composables/useAuth'

const { t } = useI18n()
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
    <RouterLink v-slot="{ isActive, href, navigate }" to="/account" custom>
      <Button as-child :variant="isActive ? 'default' : 'outline'" size="icon" :title="t('layout.headerActions.account')">
        <a :href="href" :aria-label="t('layout.headerActions.account')" @click="navigate">
          <UserCog class="size-4" />
        </a>
      </Button>
    </RouterLink>
    <RouterLink v-if="isAdmin" v-slot="{ isActive, href, navigate }" to="/settings" custom>
      <Button as-child :variant="isActive ? 'default' : 'outline'" size="icon" :title="t('layout.headerActions.settings')">
        <a :href="href" :aria-label="t('layout.headerActions.settings')" @click="navigate">
          <Settings class="size-4" />
        </a>
      </Button>
    </RouterLink>
    <ThemeToggle />
    <!-- Icon-only on phones so everything fits; the name stays "Log out". -->
    <Button variant="outline" size="sm" :aria-label="t('layout.headerActions.logOut')" class="max-sm:w-10 max-sm:px-0" @click="onLogout">
      <LogOut class="size-4 sm:hidden" />
      <span class="max-sm:hidden">{{ t('layout.headerActions.logOut') }}</span>
    </Button>
  </div>
</template>
