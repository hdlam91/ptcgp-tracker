<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

defineProps<{
  open: boolean | null
  busy: boolean
}>()

defineEmits<{ (e: 'toggle'): void }>()

const { t } = useI18n()
</script>

<template>
  <Card>
    <CardHeader>
      <CardTitle class="text-base">
        {{ t('admin.registration.title') }}
      </CardTitle>
      <CardDescription>
        <template v-if="open === null">
          {{ t('admin.registration.loading') }}
        </template>
        <template v-else-if="open">
          {{ t('admin.registration.openDescription') }}
        </template>
        <template v-else>
          {{ t('admin.registration.closedDescription') }}
        </template>
      </CardDescription>
    </CardHeader>
    <CardContent>
      <Button variant="outline" :disabled="busy || open === null" @click="$emit('toggle')">
        {{ open ? t('admin.registration.close') : t('admin.registration.open') }}
      </Button>
    </CardContent>
  </Card>
</template>
