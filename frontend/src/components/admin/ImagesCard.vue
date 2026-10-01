<script setup lang="ts">
import type { ImageMirrorStatusResponse } from '@/types/api'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import SetProgressBar from '@/components/cards/SetProgressBar.vue'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

const props = defineProps<{
  status: ImageMirrorStatusResponse | null
  busy: boolean
}>()

defineEmits<{ (e: 'download'): void }>()

const { t } = useI18n()

const running = computed(() => props.status?.state === 'Running')
const storedCount = computed(() => (props.status?.storedCards ?? 0) + (props.status?.storedPacks ?? 0))
const finished = computed(() => (props.status?.completed ?? 0) + (props.status?.failed ?? 0))

function formatBytes(bytes: number): string {
  return bytes < 1024 * 1024
    ? t('admin.images.kb', { value: Math.round(bytes / 1024) })
    : t('admin.images.mb', { value: (bytes / (1024 * 1024)).toFixed(1) })
}
</script>

<template>
  <Card class="md:col-span-2">
    <CardHeader>
      <CardTitle class="text-base">
        {{ t('admin.images.title') }}
      </CardTitle>
      <CardDescription>
        {{ t('admin.images.description') }}
      </CardDescription>
    </CardHeader>
    <CardContent class="flex flex-col gap-3">
      <p v-if="status" class="text-sm">
        <template v-if="storedCount > 0">
          <span class="font-medium">{{ status.storedCards }}</span> {{ t('admin.images.cardImagesAnd') }}
          <span class="font-medium">{{ status.storedPacks }}</span> {{ t('admin.images.packImagesStored', { bytes: formatBytes(status.storedBytes) }) }}
        </template>
        <template v-else>
          {{ t('admin.images.noneStored') }}
        </template>
      </p>

      <div v-if="running && status" class="flex flex-col gap-1" role="status">
        <p class="text-sm text-muted-foreground">
          <template v-if="status.total === 0">
            {{ t('admin.images.preparing') }}
          </template>
          <template v-else>
            {{ t('admin.images.downloadingProgress', { finished, total: status.total }) }}
          </template>
        </p>
        <SetProgressBar :owned="finished" :total="Math.max(status.total, 1)" />
      </div>

      <p v-else-if="status?.state === 'Completed'" class="text-sm text-muted-foreground">
        {{ t('admin.images.finished', { completed: status.completed, total: status.total }) }}
      </p>
      <p v-if="status?.state === 'Completed' && status.failed > 0" class="text-sm text-destructive">
        {{ t('admin.images.failedRetry', { failed: status.failed }) }}
      </p>
      <p v-if="status?.state === 'Failed'" class="text-sm text-destructive">
        {{ t('admin.images.downloadStopped', { error: status.error }) }}
      </p>

      <Button variant="outline" class="self-start" :disabled="busy || running" @click="$emit('download')">
        {{ running ? t('admin.images.downloadingButton') : storedCount > 0 ? t('admin.images.downloadMissing') : t('admin.images.downloadNew') }}
      </Button>
    </CardContent>
  </Card>
</template>
