<script setup lang="ts">
import { computed } from 'vue'
import { useLocalImage } from '@/composables/useLocalImage'
import { cn } from '@/lib/utils'
import type { PackOption } from '@/composables/useCardFilters'

const props = defineProps<{
  pack: PackOption
  class?: string
}>()

const remoteUrl = computed(() => props.pack.image ?? props.pack.image_png)
const localUrl = computed(() => {
  if (!remoteUrl.value) return undefined
  const ext = remoteUrl.value.slice(remoteUrl.value.lastIndexOf('.'))
  return `/pack-images/${props.pack.id}${ext}`
})
const { src, onError } = useLocalImage(localUrl.value, remoteUrl.value)
</script>

<template>
  <img
    v-if="src"
    :src="src"
    :alt="pack.name"
    :class="cn('object-contain', props.class)"
    loading="lazy"
    @error="onError"
  >
</template>
