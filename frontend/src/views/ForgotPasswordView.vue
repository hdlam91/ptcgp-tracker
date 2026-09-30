<script setup lang="ts">
import { ref } from 'vue'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { authService } from '@/services/authService'

const email = ref('')
const isSubmitting = ref(false)
const submitted = ref(false)

async function onSubmit() {
  isSubmitting.value = true
  try {
    await authService.forgotPassword(email.value)
  }
  finally {
    // Same message either way — the backend never reveals whether the email is registered,
    // so the UI can't either.
    isSubmitting.value = false
    submitted.value = true
  }
}
</script>

<template>
  <div class="flex min-h-screen items-center justify-center bg-muted/40 px-4">
    <Card class="w-full max-w-sm">
      <CardHeader>
        <CardTitle>Forgot password</CardTitle>
        <CardDescription>We'll email you a link to reset it.</CardDescription>
      </CardHeader>
      <CardContent>
        <template v-if="submitted">
          <p class="text-sm text-muted-foreground">
            If an account exists for {{ email }}, check your inbox for a reset link.
          </p>
        </template>
        <form v-else class="flex flex-col gap-4" @submit.prevent="onSubmit">
          <div class="flex flex-col gap-1.5">
            <Label for="email">Email</Label>
            <Input id="email" v-model="email" type="email" autocomplete="email" required />
          </div>
          <Button type="submit" :disabled="isSubmitting">
            {{ isSubmitting ? 'Sending…' : 'Send reset link' }}
          </Button>
        </form>
        <p class="mt-4 text-center text-sm text-muted-foreground">
          <RouterLink to="/login" class="font-medium text-primary underline-offset-4 hover:underline">
            Back to log in
          </RouterLink>
        </p>
      </CardContent>
    </Card>
  </div>
</template>
