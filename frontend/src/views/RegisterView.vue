<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAppConfig } from '@/composables/useAppConfig'
import { useAuth } from '@/composables/useAuth'
import { ApiError } from '@/services/httpClient'

const displayName = ref('')
const email = ref('')
const password = ref('')
const errorMessage = ref('')
const isSubmitting = ref(false)
const awaitingConfirmation = ref(false)

const { register } = useAuth()
const { registrationOpen, load: loadConfig, markRegistrationClosed } = useAppConfig()

onMounted(loadConfig)
const router = useRouter()

async function onSubmit() {
  errorMessage.value = ''
  isSubmitting.value = true
  try {
    const result = await register({ email: email.value, password: password.value, displayName: displayName.value })
    if (result.requiresEmailConfirmation) {
      awaitingConfirmation.value = true
    }
    else {
      await router.push('/')
    }
  }
  catch (error) {
    if (error instanceof ApiError && (error.body as { error?: string } | null)?.error === 'registration_closed') {
      // Closed after the page loaded — show the closed state instead of a form that can't work.
      markRegistrationClosed()
    }
    else {
      errorMessage.value = error instanceof ApiError
        ? describeRegistrationError(error)
        : 'Something went wrong. Please try again.'
    }
  }
  finally {
    isSubmitting.value = false
  }
}

function describeRegistrationError(error: ApiError): string {
  const body = error.body as { errors?: Record<string, string[]> } | null
  const firstError = body?.errors ? Object.values(body.errors)[0]?.[0] : undefined
  return firstError ?? 'Could not create your account. Please check your details.'
}
</script>

<template>
  <div class="flex min-h-screen items-center justify-center bg-muted/40 px-4">
    <Card class="w-full max-w-sm">
      <CardHeader>
        <CardTitle>
          {{ awaitingConfirmation ? 'Check your email' : registrationOpen === false ? 'Registration is closed' : 'Create an account' }}
        </CardTitle>
        <CardDescription>
          {{ awaitingConfirmation ? "You're almost done." : registrationOpen === false ? 'New accounts aren\'t being accepted right now.' : 'Start tracking your Pokémon TCG Pocket collection.' }}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <p v-if="awaitingConfirmation" class="text-sm text-muted-foreground">
          Check your inbox for a link to confirm {{ email }} before logging in.
        </p>
        <form v-else-if="registrationOpen !== false" class="flex flex-col gap-4" @submit.prevent="onSubmit">
          <div class="flex flex-col gap-1.5">
            <Label for="displayName">Display name</Label>
            <Input id="displayName" v-model="displayName" autocomplete="nickname" required />
          </div>
          <div class="flex flex-col gap-1.5">
            <Label for="email">Email</Label>
            <Input id="email" v-model="email" type="email" autocomplete="email" required />
          </div>
          <div class="flex flex-col gap-1.5">
            <Label for="password">Password</Label>
            <Input id="password" v-model="password" type="password" autocomplete="new-password" required />
          </div>
          <p v-if="errorMessage" class="text-sm text-destructive">
            {{ errorMessage }}
          </p>
          <Button type="submit" :disabled="isSubmitting">
            {{ isSubmitting ? 'Creating account…' : 'Sign up' }}
          </Button>
        </form>
        <p class="mt-4 text-center text-sm text-muted-foreground">
          Already have an account?
          <RouterLink to="/login" class="font-medium text-primary underline-offset-4 hover:underline">
            Log in
          </RouterLink>
        </p>
      </CardContent>
    </Card>
  </div>
</template>
