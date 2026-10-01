<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAppConfig } from '@/composables/useAppConfig'
import { useAuth } from '@/composables/useAuth'
import { authService } from '@/services/authService'
import { ApiError } from '@/services/httpClient'

const email = ref('')
const password = ref('')
const errorMessage = ref('')
const isSubmitting = ref(false)

// The password step, the 2FA step, and the "confirm your email first" step are separate
// screens, but only one is ever showing.
const awaitingTwoFactor = ref(false)
const code = ref('')
const isRecoveryCode = ref(false)
const rememberDevice = ref(false)
const awaitingEmailConfirmation = ref(false)
const resendSent = ref(false)

const { login, loginTwoFactor } = useAuth()
const { registrationOpen, load: loadConfig } = useAppConfig()

onMounted(loadConfig)
const router = useRouter()
const route = useRoute()

async function afterLogin() {
  const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
  await router.push(redirect)
}

async function onSubmit() {
  errorMessage.value = ''
  isSubmitting.value = true
  try {
    const result = await login({ email: email.value, password: password.value })
    if (result.requiresTwoFactor) {
      awaitingTwoFactor.value = true
    }
    else if (result.requiresEmailConfirmation) {
      awaitingEmailConfirmation.value = true
    }
    else {
      await afterLogin()
    }
  }
  catch (error) {
    errorMessage.value = error instanceof ApiError && error.status === 401
      ? 'Incorrect email or password.'
      : 'Something went wrong. Please try again.'
  }
  finally {
    isSubmitting.value = false
  }
}

async function onResendConfirmation() {
  isSubmitting.value = true
  try {
    await authService.resendConfirmation(email.value)
  }
  finally {
    isSubmitting.value = false
    resendSent.value = true
  }
}

async function onSubmitTwoFactor() {
  errorMessage.value = ''
  isSubmitting.value = true
  try {
    await loginTwoFactor({ code: code.value, isRecoveryCode: isRecoveryCode.value, rememberDevice: rememberDevice.value })
    await afterLogin()
  }
  catch (error) {
    errorMessage.value = error instanceof ApiError && error.status === 401
      ? (isRecoveryCode.value ? 'That recovery code isn\'t valid.' : 'That code isn\'t valid.')
      : 'Something went wrong. Please try again.'
  }
  finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="flex min-h-screen items-center justify-center bg-muted/40 px-4">
    <Card class="w-full max-w-sm">
      <template v-if="!awaitingTwoFactor && !awaitingEmailConfirmation">
        <CardHeader>
          <CardTitle>Log in</CardTitle>
          <CardDescription>Track your Pokémon TCG Pocket collection.</CardDescription>
        </CardHeader>
        <CardContent>
          <form class="flex flex-col gap-4" @submit.prevent="onSubmit">
            <div class="flex flex-col gap-1.5">
              <Label for="email">Email</Label>
              <Input id="email" v-model="email" type="email" autocomplete="email" required />
            </div>
            <div class="flex flex-col gap-1.5">
              <div class="flex items-center justify-between">
                <Label for="password">Password</Label>
                <RouterLink to="/forgot-password" class="text-sm text-muted-foreground underline-offset-4 hover:underline">
                  Forgot password?
                </RouterLink>
              </div>
              <Input id="password" v-model="password" type="password" autocomplete="current-password" required />
            </div>
            <p v-if="errorMessage" class="text-sm text-destructive">
              {{ errorMessage }}
            </p>
            <Button type="submit" :disabled="isSubmitting">
              {{ isSubmitting ? 'Logging in…' : 'Log in' }}
            </Button>
          </form>
          <p v-if="registrationOpen" class="mt-4 text-center text-sm text-muted-foreground">
            Don't have an account?
            <RouterLink to="/register" class="font-medium text-primary underline-offset-4 hover:underline">
              Sign up
            </RouterLink>
          </p>
        </CardContent>
      </template>

      <template v-else-if="awaitingEmailConfirmation">
        <CardHeader>
          <CardTitle>Confirm your email</CardTitle>
          <CardDescription>You need to confirm {{ email }} before you can log in.</CardDescription>
        </CardHeader>
        <CardContent class="flex flex-col gap-4">
          <p v-if="resendSent" class="text-sm text-muted-foreground">
            Check your inbox for the confirmation link.
          </p>
          <Button v-else :disabled="isSubmitting" @click="onResendConfirmation">
            {{ isSubmitting ? 'Sending…' : 'Resend confirmation email' }}
          </Button>
          <button
            type="button"
            class="text-center text-sm font-medium text-primary underline-offset-4 hover:underline"
            @click="awaitingEmailConfirmation = false; resendSent = false"
          >
            Back to log in
          </button>
        </CardContent>
      </template>

      <template v-else>
        <CardHeader>
          <CardTitle>Two-factor authentication</CardTitle>
          <CardDescription>
            {{ isRecoveryCode ? 'Enter one of your recovery codes.' : 'Enter the 6-digit code from your authenticator app.' }}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form class="flex flex-col gap-4" @submit.prevent="onSubmitTwoFactor">
            <div class="flex flex-col gap-1.5">
              <Label for="code">{{ isRecoveryCode ? 'Recovery code' : 'Code' }}</Label>
              <Input
                id="code" v-model="code" :inputmode="isRecoveryCode ? 'text' : 'numeric'"
                autocomplete="one-time-code" autofocus required
              />
            </div>
            <label class="flex items-center gap-2 text-sm">
              <input v-model="rememberDevice" type="checkbox" class="size-4 rounded border-input">
              Remember this device for 30 days
            </label>
            <p v-if="errorMessage" class="text-sm text-destructive">
              {{ errorMessage }}
            </p>
            <Button type="submit" :disabled="isSubmitting">
              {{ isSubmitting ? 'Verifying…' : 'Verify' }}
            </Button>
          </form>
          <button
            type="button"
            class="mt-4 w-full text-center text-sm font-medium text-primary underline-offset-4 hover:underline"
            @click="isRecoveryCode = !isRecoveryCode; code = ''; errorMessage = ''"
          >
            {{ isRecoveryCode ? 'Use an authenticator code instead' : 'Use a recovery code instead' }}
          </button>
        </CardContent>
      </template>
    </Card>
  </div>
</template>
