<script setup lang="ts">
import { ref } from 'vue'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAccount } from '@/composables/useAccount'

const emit = defineEmits<{ (e: 'deleted'): void }>()

const { busy, error, deleteAccount } = useAccount()

// Deleting your own account is permanent, so it takes an explicit second step and your password.
const confirming = ref(false)
const currentPassword = ref('')

async function onConfirm() {
  const result = await deleteAccount(currentPassword.value)
  if (result.ok) {
    emit('deleted')
  }
}

function cancel() {
  confirming.value = false
  currentPassword.value = ''
}
</script>

<template>
  <Card class="border-destructive/40">
    <CardHeader>
      <CardTitle class="text-base">
        Delete account
      </CardTitle>
      <CardDescription>Permanently deletes your account, your collection, and your trade list. This can't be undone.</CardDescription>
    </CardHeader>
    <CardContent>
      <Button v-if="!confirming" variant="destructive" @click="confirming = true">
        Delete my account
      </Button>
      <form v-else class="flex flex-col gap-3" @submit.prevent="onConfirm">
        <div class="flex flex-col gap-1.5">
          <Label for="delete-current-password">Enter your current password to confirm</Label>
          <Input id="delete-current-password" v-model="currentPassword" type="password" autocomplete="current-password" required />
        </div>
        <p v-if="error" class="text-sm text-destructive">
          {{ error }}
        </p>
        <div class="flex items-center gap-2">
          <Button type="submit" variant="destructive" :disabled="busy">
            {{ busy ? 'Deleting…' : 'Yes, delete everything' }}
          </Button>
          <Button type="button" variant="outline" :disabled="busy" @click="cancel">
            Cancel
          </Button>
        </div>
      </form>
    </CardContent>
  </Card>
</template>
