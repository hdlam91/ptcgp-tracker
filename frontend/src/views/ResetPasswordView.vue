<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { authService } from '@/services/authService'
import { ApiError } from '@/services/httpClient'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const email = typeof route.query.email === 'string' ? route.query.email : ''
const token = typeof route.query.token === 'string' ? route.query.token : ''

const newPassword = ref('')
const confirmPassword = ref('')
const isSubmitting = ref(false)
const errorMessage = ref('')
const mismatch = ref(false)

async function onSubmit() {
  errorMessage.value = ''
  mismatch.value = newPassword.value !== confirmPassword.value
  if (mismatch.value)
    return

  isSubmitting.value = true
  try {
    await authService.resetPassword({ email, token, newPassword: newPassword.value })
    await router.push('/login')
  }
  catch (error) {
    errorMessage.value = error instanceof ApiError ? describeResetError(error) : t('errors.generic')
  }
  finally {
    isSubmitting.value = false
  }
}

function describeResetError(error: ApiError): string {
  const body = error.body as { errors?: Record<string, string[]> } | null
  const firstError = body?.errors ? Object.values(body.errors)[0]?.[0] : undefined
  return firstError ?? t('resetPassword.genericError')
}
</script>

<template>
  <div class="flex min-h-screen items-center justify-center bg-muted/40 px-4">
    <Card class="w-full max-w-sm">
      <CardHeader>
        <CardTitle>{{ t('resetPassword.title') }}</CardTitle>
        <CardDescription>{{ t('resetPassword.description', { email }) }}</CardDescription>
      </CardHeader>
      <CardContent>
        <template v-if="!email || !token">
          <p class="text-sm text-destructive">
            {{ t('resetPassword.missingInfo') }}
          </p>
        </template>
        <form v-else class="flex flex-col gap-4" @submit.prevent="onSubmit">
          <div class="flex flex-col gap-1.5">
            <Label for="new-password">{{ t('resetPassword.newPassword') }}</Label>
            <Input id="new-password" v-model="newPassword" type="password" autocomplete="new-password" required />
          </div>
          <div class="flex flex-col gap-1.5">
            <Label for="confirm-password">{{ t('resetPassword.confirmPassword') }}</Label>
            <Input id="confirm-password" v-model="confirmPassword" type="password" autocomplete="new-password" required />
          </div>
          <p v-if="mismatch" class="text-sm text-destructive">
            {{ t('account.password.mismatch') }}
          </p>
          <p v-else-if="errorMessage" class="text-sm text-destructive">
            {{ errorMessage }}
          </p>
          <Button type="submit" :disabled="isSubmitting">
            {{ isSubmitting ? t('resetPassword.submitting') : t('resetPassword.submit') }}
          </Button>
        </form>
        <p class="mt-4 text-center text-sm text-muted-foreground">
          <RouterLink to="/forgot-password" class="font-medium text-primary underline-offset-4 hover:underline">
            {{ t('resetPassword.requestNewLink') }}
          </RouterLink>
        </p>
      </CardContent>
    </Card>
  </div>
</template>
