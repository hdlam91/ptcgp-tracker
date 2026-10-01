<script setup lang="ts">
import { WifiOff } from '@lucide/vue'
import { useI18n } from 'vue-i18n'
import { Button } from '@/components/ui/button'

const { t } = useI18n()

// A reload re-runs the startup check, and works offline too because the installed app's shell
// is served by its service worker.
function retry() {
  window.location.reload()
}
</script>

<template>
  <div role="alert" class="flex min-h-screen flex-col items-center justify-center gap-4 px-6 text-center">
    <WifiOff class="size-10 text-muted-foreground" aria-hidden="true" />
    <h1 class="text-xl font-semibold">
      {{ t('layout.serverUnreachable.title') }}
    </h1>
    <p class="max-w-sm text-sm text-muted-foreground">
      {{ t('layout.serverUnreachable.description') }}
    </p>
    <Button @click="retry">
      {{ t('layout.serverUnreachable.retry') }}
    </Button>
  </div>
</template>
