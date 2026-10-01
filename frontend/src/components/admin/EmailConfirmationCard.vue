<script setup lang="ts">
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

defineProps<{
  required: boolean | null
  smtpConfigured: boolean
  busy: boolean
}>()

defineEmits<{ (e: 'toggle'): void }>()
</script>

<template>
  <Card>
    <CardHeader>
      <CardTitle class="text-base">
        Email confirmation
      </CardTitle>
      <CardDescription>
        <template v-if="required === null">
          Loading…
        </template>
        <template v-else-if="required">
          Required — new accounts must click a link in their email before they can log in.
        </template>
        <template v-else>
          Off — new accounts can log in right away.
        </template>
      </CardDescription>
    </CardHeader>
    <CardContent class="flex flex-col gap-3">
      <p v-if="!smtpConfigured" class="rounded-md border border-amber-300 bg-amber-50 px-3 py-2 text-sm text-amber-800 dark:border-amber-800 dark:bg-amber-950 dark:text-amber-200">
        No SMTP server is configured, so no confirmation email can actually send. Configured admin accounts are always exempt, so turning this on can't lock you out — but it will lock out everyone else until SMTP is set up.
      </p>
      <Button variant="outline" :disabled="busy || required === null" class="self-start" @click="$emit('toggle')">
        {{ required ? 'Turn off email confirmation' : 'Require email confirmation' }}
      </Button>
    </CardContent>
  </Card>
</template>
