<script setup lang="ts">
import { useRouter } from 'vue-router'
import ChangeEmailCard from '@/components/account/ChangeEmailCard.vue'
import ChangePasswordCard from '@/components/account/ChangePasswordCard.vue'
import DeleteAccountCard from '@/components/account/DeleteAccountCard.vue'
import TwoFactorCard from '@/components/account/TwoFactorCard.vue'
import { useAuth } from '@/composables/useAuth'

const { currentUser, fetchCurrentUser } = useAuth()
const router = useRouter()

async function onDeleted() {
  // The delete endpoint already signs the session out server-side, so calling the /logout
  // endpoint again would 401 (it requires an authenticated session). fetchCurrentUser's /me
  // call handles a 401 as "not logged in" without throwing, which clears currentUser here too.
  await fetchCurrentUser()
  await router.push('/login')
}
</script>

<template>
  <div class="mx-auto max-w-3xl p-4 sm:p-6">
    <h1 class="text-2xl font-semibold">
      Account
    </h1>
    <p class="mt-1 text-muted-foreground">
      Manage your login details and security.
    </p>

    <div class="mt-6 flex flex-col gap-4">
      <ChangePasswordCard />
      <ChangeEmailCard v-if="currentUser" :current-email="currentUser.email" @changed="fetchCurrentUser" />
      <TwoFactorCard />
      <DeleteAccountCard @deleted="onDeleted" />
    </div>
  </div>
</template>
