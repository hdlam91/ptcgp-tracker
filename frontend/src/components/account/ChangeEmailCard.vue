<script setup lang="ts">
import { ref } from 'vue'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAccount } from '@/composables/useAccount'

const props = defineProps<{
  currentEmail: string
}>()

const emit = defineEmits<{ (e: 'changed', newEmail: string): void }>()

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
        Email
      </CardTitle>
      <CardDescription>Signed in as {{ props.currentEmail }}.</CardDescription>
    </CardHeader>
    <CardContent>
      <form class="flex flex-col gap-3" @submit.prevent="onSubmit">
        <div class="flex flex-col gap-1.5">
          <Label for="new-email">New email</Label>
          <Input id="new-email" v-model="newEmail" type="email" autocomplete="email" required />
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="email-current-password">Current password</Label>
          <Input id="email-current-password" v-model="currentPassword" type="password" autocomplete="current-password" required />
        </div>
        <p v-if="error" class="text-sm text-destructive">
          {{ error }}
        </p>
        <p v-else-if="done" class="text-sm text-primary">
          Email changed.
        </p>
        <Button type="submit" :disabled="busy" class="self-start">
          {{ busy ? 'Changing…' : 'Change email' }}
        </Button>
      </form>
    </CardContent>
  </Card>
</template>
