<script setup lang="ts">
import type { PackOption } from '@/composables/useCardFilters'
import { computed } from 'vue'
import { useHoverPreview } from '@/composables/useHoverPreview'
import { useLocalImage } from '@/composables/useLocalImage'
import { cn } from '@/lib/utils'

const props = withDefaults(defineProps<{
  pack: PackOption
  class?: string
  /** Show an enlarged copy of the pack art next to the mouse pointer while hovering. */
  preview?: boolean
  /** Defaults to the pack's name; pass "" when the icon is purely decorative (e.g. redundant
   * with adjacent text), so it doesn't get pulled into a containing link or button's accessible name. */
  alt?: string
}>(), {
  preview: false,
  alt: undefined,
})

const remoteUrl = computed(() => props.pack.image ?? props.pack.image_png)
const localUrl = computed(() => {
  if (!remoteUrl.value)
    return undefined
  const ext = remoteUrl.value.slice(remoteUrl.value.lastIndexOf('.'))
  return `/pack-images/${props.pack.id}${ext}`
})
const { src, onError } = useLocalImage(localUrl.value, remoteUrl.value)

const PREVIEW_WIDTH = 176
const PREVIEW_HEIGHT = 288
const { hovering, position, onPointerMove, onPointerLeave } = useHoverPreview({ width: PREVIEW_WIDTH, height: PREVIEW_HEIGHT })
</script>

<template>
  <img
    v-if="src"
    :src="src"
    :alt="props.alt ?? pack.name"
    :class="cn('object-contain', props.class)"
    loading="lazy"
    @error="onError"
    @pointermove="preview && onPointerMove($event)"
    @pointerenter="preview && onPointerMove($event)"
    @pointerleave="onPointerLeave"
  >
  <!-- Teleported so the enlarged image isn't clipped by, or click-blocking inside, the card it sits in. -->
  <Teleport v-if="preview && src" to="body">
    <img
      v-if="hovering"
      :src="src"
      alt=""
      aria-hidden="true"
      class="pointer-events-none fixed z-50 object-contain drop-shadow-xl"
      :style="{ left: `${position.left}px`, top: `${position.top}px`, width: `${PREVIEW_WIDTH}px`, maxHeight: `${PREVIEW_HEIGHT}px` }"
    >
  </Teleport>
</template>
