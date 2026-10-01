<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAppConfig } from '@/composables/useAppConfig'
import { useAuth } from '@/composables/useAuth'
import { ApiError } from '@/services/httpClient'

const { t } = useI18n()
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
        : t('errors.generic')
    }
  }
  finally {
    isSubmitting.value = false
  }
}

function describeRegistrationError(error: ApiError): string {
  const body = error.body as { errors?: Record<string, string[]> } | null
  const firstError = body?.errors ? Object.values(body.errors)[0]?.[0] : undefined
  return firstError ?? t('register.genericError')
}
</script>

<template>
  <div class="flex min-h-screen items-center justify-center bg-muted/40 px-4">
    <Card class="w-full max-w-sm">
      <CardHeader>
        <CardTitle>
          {{ awaitingConfirmation ? t('register.checkYourEmailTitle') : registrationOpen === false ? t('register.closedTitle') : t('register.title') }}
        </CardTitle>
        <CardDescription>
          {{ awaitingConfirmation ? t('register.checkYourEmailDescription') : registrationOpen === false ? t('register.closedDescription') : t('register.description') }}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <p v-if="awaitingConfirmation" class="text-sm text-muted-foreground">
          {{ t('register.confirmEmailInstructions', { email }) }}
        </p>
        <form v-else-if="registrationOpen !== false" class="flex flex-col gap-4" @submit.prevent="onSubmit">
          <div class="flex flex-col gap-1.5">
            <Label for="displayName">{{ t('register.displayName') }}</Label>
            <Input id="displayName" v-model="displayName" autocomplete="nickname" required />
          </div>
          <div class="flex flex-col gap-1.5">
            <Label for="email">{{ t('register.email') }}</Label>
            <Input id="email" v-model="email" type="email" autocomplete="email" required />
          </div>
          <div class="flex flex-col gap-1.5">
            <Label for="password">{{ t('register.password') }}</Label>
            <Input id="password" v-model="password" type="password" autocomplete="new-password" required />
          </div>
          <p v-if="errorMessage" class="text-sm text-destructive">
            {{ errorMessage }}
          </p>
          <Button type="submit" :disabled="isSubmitting">
            {{ isSubmitting ? t('register.submitting') : t('register.submit') }}
          </Button>
        </form>
        <p class="mt-4 text-center text-sm text-muted-foreground">
          {{ t('register.haveAccount') }}
          <RouterLink to="/login" class="font-medium text-primary underline-offset-4 hover:underline">
            {{ t('register.logIn') }}
          </RouterLink>
        </p>
      </CardContent>
    </Card>
  </div>
</template>
