<script setup lang="ts">
import type { CatalogStatusResponse } from '@/types/api'
import { useI18n } from 'vue-i18n'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

defineProps<{
  catalog: CatalogStatusResponse | null
  busy: boolean
}>()

defineEmits<{ (e: 'refresh'): void }>()

const { t } = useI18n()
</script>

<template>
  <Card>
    <CardHeader>
      <CardTitle class="text-base">
        {{ t('admin.catalog.title') }}
      </CardTitle>
      <CardDescription>
        {{ t('admin.catalog.description') }}
      </CardDescription>
    </CardHeader>
    <CardContent class="flex flex-col gap-3">
      <dl v-if="catalog" class="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-sm">
        <dt class="text-muted-foreground">
          {{ t('admin.catalog.datasetVersion') }}
        </dt>
        <dd>{{ catalog.repoTag }}</dd>
        <dt class="text-muted-foreground">
          {{ t('admin.catalog.cardsLoaded') }}
        </dt>
        <dd>{{ catalog.cardCount }}</dd>
        <dt class="text-muted-foreground">
          {{ t('admin.catalog.lastRefreshed') }}
        </dt>
        <dd>{{ catalog.lastRefreshedAt ? new Date(catalog.lastRefreshedAt).toLocaleString() : t('admin.catalog.never') }}</dd>
      </dl>
      <p v-if="catalog?.lastError" class="text-sm text-destructive">
        {{ t('admin.catalog.lastRefreshFailed', { error: catalog.lastError }) }}
      </p>
      <Button variant="outline" class="self-start" :disabled="busy" @click="$emit('refresh')">
        {{ busy ? t('admin.catalog.working') : t('admin.catalog.refresh') }}
      </Button>
    </CardContent>
  </Card>
</template>
