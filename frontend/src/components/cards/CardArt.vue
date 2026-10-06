<script setup lang="ts">
import type { CardCatalogEntry } from '@/types/catalog'
import { computed } from 'vue'
import { useHoverPreview } from '@/composables/useHoverPreview'
import { useLocalImage } from '@/composables/useLocalImage'
import { cn } from '@/lib/utils'

const props = withDefaults(defineProps<{
  card: CardCatalogEntry
  class?: string
  /** Show an enlarged copy of the card art next to the mouse pointer while hovering. */
  preview?: boolean
  /** Defaults to the card's name; pass "" when the image is purely decorative (e.g. redundant
   * with adjacent text), so it doesn't get pulled into a containing link or button's accessible name. */
  alt?: string
}>(), {
  preview: false,
  alt: undefined,
})

const remoteUrl = computed(() => props.card.image ?? props.card.image_png)
const localUrl = computed(() => {
  if (!remoteUrl.value)
    return undefined
  const ext = remoteUrl.value.slice(remoteUrl.value.lastIndexOf('.'))
  return `/card-images/${props.card.id}${ext}`
})
// Not reactive to `card` on purpose: the parent keys this component by card id.
const { src, onError } = useLocalImage(localUrl.value, remoteUrl.value)

const PREVIEW_WIDTH = 180
const PREVIEW_HEIGHT = 252
const { hovering, position, onPointerMove, onPointerLeave } = useHoverPreview({ width: PREVIEW_WIDTH, height: PREVIEW_HEIGHT })
</script>

<template>
  <div
    :class="cn('aspect-[5/7] overflow-hidden rounded-xl bg-muted', props.class)"
    @pointermove="preview && onPointerMove($event)"
    @pointerenter="preview && onPointerMove($event)"
    @pointerleave="onPointerLeave"
  >
    <img v-if="src" :src="src" :alt="props.alt ?? card.name" class="h-full w-full object-contain" @error="onError">
  </div>
  <!-- Teleported so the enlarged image isn't clipped by, or click-blocking inside, the card it sits in. -->
  <Teleport v-if="preview && src" to="body">
    <img
      v-if="hovering"
      :src="src"
      alt=""
      aria-hidden="true"
      class="pointer-events-none fixed z-50 rounded-lg object-contain drop-shadow-xl"
      :style="{ left: `${position.left}px`, top: `${position.top}px`, width: `${PREVIEW_WIDTH}px`, maxHeight: `${PREVIEW_HEIGHT}px` }"
    >
  </Teleport>
</template>
