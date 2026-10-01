<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAccount } from '@/composables/useAccount'

const { t } = useI18n()
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
        {{ t('account.password.title') }}
      </CardTitle>
      <CardDescription>{{ t('account.password.description') }}</CardDescription>
    </CardHeader>
    <CardContent>
      <form class="flex flex-col gap-3" @submit.prevent="onSubmit">
        <div class="flex flex-col gap-1.5">
          <Label for="current-password">{{ t('account.password.currentPassword') }}</Label>
          <Input id="current-password" v-model="currentPassword" type="password" autocomplete="current-password" required />
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="new-password">{{ t('account.password.newPassword') }}</Label>
          <Input id="new-password" v-model="newPassword" type="password" autocomplete="new-password" required />
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="confirm-password">{{ t('account.password.confirmPassword') }}</Label>
          <Input id="confirm-password" v-model="confirmPassword" type="password" autocomplete="new-password" required />
        </div>
        <p v-if="mismatch" class="text-sm text-destructive">
          {{ t('account.password.mismatch') }}
        </p>
        <p v-else-if="error" class="text-sm text-destructive">
          {{ error }}
        </p>
        <p v-else-if="done" class="text-sm text-primary">
          {{ t('account.password.changed') }}
        </p>
        <Button type="submit" :disabled="busy" class="self-start">
          {{ busy ? t('account.password.submitting') : t('account.password.submit') }}
        </Button>
      </form>
    </CardContent>
  </Card>
</template>
