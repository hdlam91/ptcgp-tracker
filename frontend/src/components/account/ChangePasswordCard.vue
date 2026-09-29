<script setup lang="ts">
import { ref } from 'vue'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAccount } from '@/composables/useAccount'

const { busy, error, changePassword } = useAccount()

const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const done = ref(false)
const mismatch = ref(false)

async function onSubmit() {
  done.value = false
  mismatch.value = newPassword.value !== confirmPassword.value
  if (mismatch.value)
    return

  const result = await changePassword(currentPassword.value, newPassword.value)
  if (result.ok) {
    currentPassword.value = ''
    newPassword.value = ''
    confirmPassword.value = ''
    done.value = true
  }
}
</script>

<template>
  <Card>
    <CardHeader>
      <CardTitle class="text-base">
        Password
      </CardTitle>
      <CardDescription>Change the password you log in with.</CardDescription>
    </CardHeader>
    <CardContent>
      <form class="flex flex-col gap-3" @submit.prevent="onSubmit">
        <div class="flex flex-col gap-1.5">
          <Label for="current-password">Current password</Label>
          <Input id="current-password" v-model="currentPassword" type="password" autocomplete="current-password" required />
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="new-password">New password</Label>
          <Input id="new-password" v-model="newPassword" type="password" autocomplete="new-password" required />
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="confirm-password">Confirm new password</Label>
          <Input id="confirm-password" v-model="confirmPassword" type="password" autocomplete="new-password" required />
        </div>
        <p v-if="mismatch" class="text-sm text-destructive">
          Those passwords don't match.
        </p>
        <p v-else-if="error" class="text-sm text-destructive">
          {{ error }}
        </p>
        <p v-else-if="done" class="text-sm text-primary">
          Password changed.
        </p>
        <Button type="submit" :disabled="busy" class="self-start">
          {{ busy ? 'Changing…' : 'Change password' }}
        </Button>
      </form>
    </CardContent>
  </Card>
</template>
