<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAccount } from '@/composables/useAccount'

const props = defineProps<{
  currentEmail: string
}>()

const emit = defineEmits<{ (e: 'changed', newEmail: string): void }>()

const { t } = useI18n()
const { busy, error, changeEmail } = useAccount()

const newEmail = ref('')
const currentPassword = ref('')
const done = ref(false)

async function onSubmit() {
  done.value = false
  const result = await changeEmail(newEmail.value, currentPassword.value)
  if (result.ok) {
    emit('changed', newEmail.value)
    newEmail.value = ''
    currentPassword.value = ''
    done.value = true
  }
}
</script>

<template>
  <Card>
    <CardHeader>
      <CardTitle class="text-base">
        {{ t('account.email.title') }}
      </CardTitle>
      <CardDescription>{{ t('account.email.signedInAs', { email: props.currentEmail }) }}</CardDescription>
    </CardHeader>
    <CardContent>
      <form class="flex flex-col gap-3" @submit.prevent="onSubmit">
        <div class="flex flex-col gap-1.5">
          <Label for="new-email">{{ t('account.email.newEmail') }}</Label>
          <Input id="new-email" v-model="newEmail" type="email" autocomplete="email" required />
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="email-current-password">{{ t('account.email.currentPassword') }}</Label>
          <Input id="email-current-password" v-model="currentPassword" type="password" autocomplete="current-password" required />
        </div>
        <p v-if="error" class="text-sm text-destructive">
          {{ error }}
        </p>
        <p v-else-if="done" class="text-sm text-primary">
          {{ t('account.email.changed') }}
        </p>
        <Button type="submit" :disabled="busy" class="self-start">
          {{ busy ? t('account.email.submitting') : t('account.email.submit') }}
        </Button>
      </form>
    </CardContent>
  </Card>
</template>
