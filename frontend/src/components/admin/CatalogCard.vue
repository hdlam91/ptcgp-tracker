<script setup lang="ts">
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import type { CatalogStatusResponse } from '@/types/api'

defineProps<{
  catalog: CatalogStatusResponse | null
  busy: boolean
}>()

defineEmits<{ (e: 'refresh'): void }>()
</script>

<template>
  <Card>
    <CardHeader>
      <CardTitle class="text-base">
        Card data
      </CardTitle>
      <CardDescription>
        What the server uses to validate card IDs. The card art and details you see on the site come from the bundled dataset.
      </CardDescription>
    </CardHeader>
    <CardContent class="flex flex-col gap-3">
      <dl v-if="catalog" class="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-sm">
        <dt class="text-muted-foreground">
          Dataset version
        </dt>
        <dd>{{ catalog.repoTag }}</dd>
        <dt class="text-muted-foreground">
          Cards loaded
        </dt>
        <dd>{{ catalog.cardCount }}</dd>
        <dt class="text-muted-foreground">
          Last refreshed
        </dt>
        <dd>{{ catalog.lastRefreshedAt ? new Date(catalog.lastRefreshedAt).toLocaleString() : 'Never' }}</dd>
      </dl>
      <p v-if="catalog?.lastError" class="text-sm text-destructive">
        Last refresh failed: {{ catalog.lastError }}
      </p>
      <Button variant="outline" class="self-start" :disabled="busy" @click="$emit('refresh')">
        {{ busy ? 'Working…' : 'Refresh card data' }}
      </Button>
    </CardContent>
  </Card>
</template>
