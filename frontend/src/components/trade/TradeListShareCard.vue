<script setup lang="ts">
import { Check, Copy, ExternalLink } from '@lucide/vue'
import { computed, onMounted, ref } from 'vue'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { tradeListService } from '@/services/tradeListService'

const handle = ref<string | null>(null)
const loading = ref(true)
const busy = ref(false)
const copied = ref(false)

onMounted(async () => {
  const status = await tradeListService.getShareStatus()
  handle.value = status.handle
  loading.value = false
})

const shareUrl = computed(() => (handle.value ? `${window.location.origin}/share/${handle.value}` : null))

async function enable() {
  busy.value = true
  try {
    const status = await tradeListService.enableSharing()
    handle.value = status.handle
  }
  finally {
    busy.value = false
  }
}

async function disable() {
  busy.value = true
  try {
    await tradeListService.disableSharing()
    handle.value = null
  }
  finally {
    busy.value = false
  }
}

async function copyLink() {
  if (!shareUrl.value) return
  await navigator.clipboard.writeText(shareUrl.value)
  copied.value = true
  setTimeout(() => { copied.value = false }, 1500)
}
</script>

<template>
  <Card v-if="!loading">
    <CardHeader>
      <CardTitle class="text-base">
        Share your trade list
      </CardTitle>
      <CardDescription>
        Anyone with the link can see what you want and what you're offering. They can't edit anything.
      </CardDescription>
    </CardHeader>
    <CardContent>
      <div v-if="handle" class="flex flex-col gap-2 sm:flex-row sm:items-center">
        <input
          :value="shareUrl"
          readonly
          class="h-10 flex-1 rounded-md border border-input bg-muted px-3 text-sm text-muted-foreground"
          @focus="($event.target as HTMLInputElement).select()"
        >
        <div class="flex gap-2">
          <!-- A real link (not a button) so it can be opened in a new tab, copied, or middle-clicked. -->
          <Button as-child variant="outline" size="sm">
            <a :href="shareUrl ?? undefined" target="_blank" rel="noopener">
              <ExternalLink class="size-3.5" />
              Open
            </a>
          </Button>
          <Button variant="outline" size="sm" @click="copyLink">
            <Check v-if="copied" class="size-3.5" />
            <Copy v-else class="size-3.5" />
            {{ copied ? 'Copied' : 'Copy' }}
          </Button>
          <Button variant="outline" size="sm" :disabled="busy" @click="disable">
            Stop sharing
          </Button>
        </div>
      </div>
      <Button v-else :disabled="busy" @click="enable">
        Create share link
      </Button>
    </CardContent>
  </Card>
</template>
