<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

defineProps<{
  required: boolean | null
  smtpConfigured: boolean
  busy: boolean
}>()

defineEmits<{ (e: 'toggle'): void }>()

const { t } = useI18n()
</script>

<template>
  <Card>
    <CardHeader>
      <CardTitle class="text-base">
        {{ t('admin.emailConfirmation.title') }}
      </CardTitle>
      <CardDescription>
        <template v-if="required === null">
          {{ t('admin.emailConfirmation.loading') }}
        </template>
        <template v-else-if="required">
          {{ t('admin.emailConfirmation.requiredDescription') }}
        </template>
        <template v-else>
          {{ t('admin.emailConfirmation.offDescription') }}
        </template>
      </CardDescription>
    </CardHeader>
    <CardContent class="flex flex-col gap-3">
      <p v-if="!smtpConfigured" class="rounded-md border border-amber-300 bg-amber-50 px-3 py-2 text-sm text-amber-800 dark:border-amber-800 dark:bg-amber-950 dark:text-amber-200">
        {{ t('admin.emailConfirmation.smtpWarning') }}
      </p>
      <Button variant="outline" :disabled="busy || required === null" class="self-start" @click="$emit('toggle')">
        {{ required ? t('admin.emailConfirmation.turnOff') : t('admin.emailConfirmation.turnOn') }}
      </Button>
    </CardContent>
  </Card>
</template>
