<script setup lang="ts">
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

defineProps<{
  open: boolean | null
  busy: boolean
}>()

defineEmits<{ (e: 'toggle'): void }>()
</script>

<template>
  <Card>
    <CardHeader>
      <CardTitle class="text-base">
        New registrations
      </CardTitle>
      <CardDescription>
        <template v-if="open === null">
          Loading…
        </template>
        <template v-else-if="open">
          Open — anyone can create an account.
        </template>
        <template v-else>
          Closed — the sign-up page is disabled. Existing users can still log in.
        </template>
      </CardDescription>
    </CardHeader>
    <CardContent>
      <Button variant="outline" :disabled="busy || open === null" @click="$emit('toggle')">
        {{ open ? 'Close registration' : 'Open registration' }}
      </Button>
    </CardContent>
  </Card>
</template>
