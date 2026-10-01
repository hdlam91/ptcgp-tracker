<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAppConfig } from '@/composables/useAppConfig'
import { useAuth } from '@/composables/useAuth'
import { authService } from '@/services/authService'
import { ApiError } from '@/services/httpClient'

const { t } = useI18n()
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
      ? t('login.incorrectCredentials')
      : t('errors.generic')
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
      ? (isRecoveryCode.value ? t('login.incorrectRecoveryCode') : t('login.incorrectCode'))
      : t('errors.generic')
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
          <CardTitle>{{ t('login.title') }}</CardTitle>
          <CardDescription>{{ t('login.description') }}</CardDescription>
        </CardHeader>
        <CardContent>
          <form class="flex flex-col gap-4" @submit.prevent="onSubmit">
            <div class="flex flex-col gap-1.5">
              <Label for="email">{{ t('login.email') }}</Label>
              <Input id="email" v-model="email" type="email" autocomplete="email" required />
            </div>
            <div class="flex flex-col gap-1.5">
              <div class="flex items-center justify-between">
                <Label for="password">{{ t('login.password') }}</Label>
                <RouterLink to="/forgot-password" class="text-sm text-muted-foreground underline-offset-4 hover:underline">
                  {{ t('login.forgotPassword') }}
                </RouterLink>
              </div>
              <Input id="password" v-model="password" type="password" autocomplete="current-password" required />
            </div>
            <p v-if="errorMessage" class="text-sm text-destructive">
              {{ errorMessage }}
            </p>
            <Button type="submit" :disabled="isSubmitting">
              {{ isSubmitting ? t('login.submitting') : t('login.submit') }}
            </Button>
          </form>
          <p v-if="registrationOpen" class="mt-4 text-center text-sm text-muted-foreground">
            {{ t('login.noAccount') }}
            <RouterLink to="/register" class="font-medium text-primary underline-offset-4 hover:underline">
              {{ t('login.signUp') }}
            </RouterLink>
          </p>
        </CardContent>
      </template>

      <template v-else-if="awaitingEmailConfirmation">
        <CardHeader>
          <CardTitle>{{ t('login.confirmEmailTitle') }}</CardTitle>
          <CardDescription>{{ t('login.confirmEmailDescription', { email }) }}</CardDescription>
        </CardHeader>
        <CardContent class="flex flex-col gap-4">
          <p v-if="resendSent" class="text-sm text-muted-foreground">
            {{ t('login.checkInbox') }}
          </p>
          <Button v-else :disabled="isSubmitting" @click="onResendConfirmation">
            {{ isSubmitting ? t('login.resending') : t('login.resendConfirmation') }}
          </Button>
          <button
            type="button"
            class="text-center text-sm font-medium text-primary underline-offset-4 hover:underline"
            @click="awaitingEmailConfirmation = false; resendSent = false"
          >
            {{ t('login.backToLogin') }}
          </button>
        </CardContent>
      </template>

      <template v-else>
        <CardHeader>
          <CardTitle>{{ t('login.twoFactorTitle') }}</CardTitle>
          <CardDescription>
            {{ isRecoveryCode ? t('login.enterRecoveryCode') : t('login.enterAuthenticatorCode') }}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form class="flex flex-col gap-4" @submit.prevent="onSubmitTwoFactor">
            <div class="flex flex-col gap-1.5">
              <Label for="code">{{ isRecoveryCode ? t('login.recoveryCode') : t('login.code') }}</Label>
              <Input
                id="code" v-model="code" :inputmode="isRecoveryCode ? 'text' : 'numeric'"
                autocomplete="one-time-code" autofocus required
              />
            </div>
            <label class="flex items-center gap-2 text-sm">
              <input v-model="rememberDevice" type="checkbox" class="size-4 rounded border-input">
              {{ t('login.rememberDevice') }}
            </label>
            <p v-if="errorMessage" class="text-sm text-destructive">
              {{ errorMessage }}
            </p>
            <Button type="submit" :disabled="isSubmitting">
              {{ isSubmitting ? t('login.verifying') : t('login.verify') }}
            </Button>
          </form>
          <button
            type="button"
            class="mt-4 w-full text-center text-sm font-medium text-primary underline-offset-4 hover:underline"
            @click="isRecoveryCode = !isRecoveryCode; code = ''; errorMessage = ''"
          >
            {{ isRecoveryCode ? t('login.useAuthenticatorCodeInstead') : t('login.useRecoveryCodeInstead') }}
          </button>
        </CardContent>
      </template>
    </Card>
  </div>
</template>
