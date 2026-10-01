<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useAuth } from '@/composables/useAuth'
import { authService } from '@/services/authService'

const route = useRoute()
const email = typeof route.query.email === 'string' ? route.query.email : ''
const token = typeof route.query.token === 'string' ? route.query.token : ''

const { confirmEmail } = useAuth()

const status = ref<'confirming' | 'confirmed' | 'failed'>('confirming')
const resendSent = ref(false)

onMounted(async () => {
  if (!email || !token) {
    status.value = 'failed'
    return
  }

  try {
    await confirmEmail({ email, token })
    status.value = 'confirmed'
  }
  catch {
    status.value = 'failed'
  }
})

async function onResend() {
  await authService.resendConfirmation(email)
  resendSent.value = true
}
</script>

<template>
  <div class="flex min-h-screen items-center justify-center bg-muted/40 px-4">
    <Card class="w-full max-w-sm">
      <CardHeader>
        <CardTitle>{{ status === 'confirmed' ? 'Email confirmed' : 'Confirm your email' }}</CardTitle>
        <CardDescription v-if="status === 'confirming'">
          One moment…
        </CardDescription>
        <CardDescription v-else-if="status === 'confirmed'">
          You're all set — you can log in now.
        </CardDescription>
        <CardDescription v-else>
          That confirmation link is invalid or has expired.
        </CardDescription>
      </CardHeader>
      <CardContent v-if="status === 'confirmed'">
        <RouterLink v-slot="{ href, navigate }" to="/" custom>
          <Button as-child class="w-full">
            <a :href="href" @click="navigate">Go to the app</a>
          </Button>
        </RouterLink>
      </CardContent>
      <CardContent v-else-if="status === 'failed'" class="flex flex-col gap-4">
        <p v-if="resendSent" class="text-sm text-muted-foreground">
          If that email needs confirming, check your inbox for a new link.
        </p>
        <Button v-else-if="email" @click="onResend">
          Send a new confirmation email
        </Button>
        <p class="text-center text-sm text-muted-foreground">
          <RouterLink to="/login" class="font-medium text-primary underline-offset-4 hover:underline">
            Back to log in
          </RouterLink>
        </p>
      </CardContent>
    </Card>
  </div>
</template>
