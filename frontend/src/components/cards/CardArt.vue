<script setup lang="ts">
import { computed } from 'vue'
import { useLocalImage } from '@/composables/useLocalImage'
import type { CardCatalogEntry } from '@/types/catalog'

const props = defineProps<{ card: CardCatalogEntry }>()

const remoteUrl = computed(() => props.card.image ?? props.card.image_png)
const localUrl = computed(() => {
  if (!remoteUrl.value) return undefined
  const ext = remoteUrl.value.slice(remoteUrl.value.lastIndexOf('.'))
  return `/card-images/${props.card.id}${ext}`
})
// Not reactive to `card` on purpose: the parent keys this component by card id.
const { src, onError } = useLocalImage(localUrl.value, remoteUrl.value)
</script>

<template>
  <div class="aspect-[5/7] overflow-hidden rounded-xl bg-muted">
    <img v-if="src" :src="src" :alt="card.name" class="h-full w-full object-contain" @error="onError">
  </div>
</template>
