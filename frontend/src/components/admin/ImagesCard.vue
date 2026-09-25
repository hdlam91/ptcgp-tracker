<script setup lang="ts">
import { computed } from 'vue'
import SetProgressBar from '@/components/cards/SetProgressBar.vue'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import type { ImageMirrorStatusResponse } from '@/types/api'

const props = defineProps<{
  status: ImageMirrorStatusResponse | null
  busy: boolean
}>()

defineEmits<{ (e: 'download'): void }>()

const running = computed(() => props.status?.state === 'Running')
const storedCount = computed(() => (props.status?.storedCards ?? 0) + (props.status?.storedPacks ?? 0))
const finished = computed(() => (props.status?.completed ?? 0) + (props.status?.failed ?? 0))

function formatBytes(bytes: number): string {
  return bytes < 1024 * 1024 ? `${Math.round(bytes / 1024)} KB` : `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}
</script>

<template>
  <Card class="md:col-span-2">
    <CardHeader>
      <CardTitle class="text-base">
        Card images
      </CardTitle>
      <CardDescription>
        By default, card art loads from GitHub in each visitor's browser. Download it to this server (roughly 135 MB) and the site serves its own images, so it no longer depends on GitHub for art.
      </CardDescription>
    </CardHeader>
    <CardContent class="flex flex-col gap-3">
      <p v-if="status" class="text-sm">
        <template v-if="storedCount > 0">
          <span class="font-medium">{{ status.storedCards }}</span> card images and
          <span class="font-medium">{{ status.storedPacks }}</span> pack images stored here ({{ formatBytes(status.storedBytes) }}).
        </template>
        <template v-else>
          No images are stored on this server yet.
        </template>
      </p>

      <div v-if="running && status" class="flex flex-col gap-1" role="status">
        <p class="text-sm text-muted-foreground">
          <template v-if="status.total === 0">
            Preparing the list of images…
          </template>
          <template v-else>
            Downloading… {{ finished }} of {{ status.total }}
          </template>
        </p>
        <SetProgressBar :owned="finished" :total="Math.max(status.total, 1)" />
      </div>

      <p v-else-if="status?.state === 'Completed'" class="text-sm text-muted-foreground">
        Finished: {{ status.completed }} of {{ status.total }} images are stored.
      </p>
      <p v-if="status?.state === 'Completed' && status.failed > 0" class="text-sm text-destructive">
        {{ status.failed }} couldn't be downloaded. Run it again to retry just those.
      </p>
      <p v-if="status?.state === 'Failed'" class="text-sm text-destructive">
        The download stopped: {{ status.error }}
      </p>

      <Button variant="outline" class="self-start" :disabled="busy || running" @click="$emit('download')">
        {{ running ? 'Downloading…' : storedCount > 0 ? 'Download missing images' : 'Download card images' }}
      </Button>
    </CardContent>
  </Card>
</template>
