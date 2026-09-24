<script setup lang="ts">
import { WifiOff } from '@lucide/vue'
import { Button } from '@/components/ui/button'

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
      Can't reach the server
    </h1>
    <p class="max-w-sm text-sm text-muted-foreground">
      Check your connection and try again. Your collection is stored on the server, so it will be there when you're back online.
    </p>
    <Button @click="retry">
      Try again
    </Button>
  </div>
</template>
