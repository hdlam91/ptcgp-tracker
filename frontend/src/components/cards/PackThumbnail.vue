<script setup lang="ts">
import { computed, ref } from 'vue'
import { useLocalImage } from '@/composables/useLocalImage'
import { cn } from '@/lib/utils'
import type { PackOption } from '@/composables/useCardFilters'

const props = withDefaults(defineProps<{
  pack: PackOption
  class?: string
  /** Show an enlarged copy of the pack art next to the mouse pointer while hovering. */
  preview?: boolean
}>(), {
  preview: false,
})

const remoteUrl = computed(() => props.pack.image ?? props.pack.image_png)
const localUrl = computed(() => {
  if (!remoteUrl.value) return undefined
  const ext = remoteUrl.value.slice(remoteUrl.value.lastIndexOf('.'))
  return `/pack-images/${props.pack.id}${ext}`
})
const { src, onError } = useLocalImage(localUrl.value, remoteUrl.value)

const PREVIEW_WIDTH = 176
const PREVIEW_HEIGHT = 288
const CURSOR_GAP = 16

const hovering = ref(false)
const previewPosition = ref({ left: 0, top: 0 })

function onPointerMove(event: PointerEvent) {
  // Hover previews are a mouse affordance; on touch, a tap shouldn't leave one stuck open.
  if (!props.preview || event.pointerType !== 'mouse') return
  hovering.value = true

  // Sit to the lower right of the cursor, flipping to the other side near a viewport edge.
  const fitsRight = event.clientX + CURSOR_GAP + PREVIEW_WIDTH <= window.innerWidth
  const fitsBelow = event.clientY + CURSOR_GAP + PREVIEW_HEIGHT <= window.innerHeight
  previewPosition.value = {
    left: fitsRight ? event.clientX + CURSOR_GAP : Math.max(0, event.clientX - CURSOR_GAP - PREVIEW_WIDTH),
    top: fitsBelow ? event.clientY + CURSOR_GAP : Math.max(0, event.clientY - CURSOR_GAP - PREVIEW_HEIGHT),
  }
}
</script>

<template>
  <img
    v-if="src"
    :src="src"
    :alt="pack.name"
    :class="cn('object-contain', props.class)"
    loading="lazy"
    @error="onError"
    @pointermove="onPointerMove"
    @pointerenter="onPointerMove"
    @pointerleave="hovering = false"
  >
  <!-- Teleported so the enlarged image isn't clipped by, or click-blocking inside, the card it sits in. -->
  <Teleport v-if="preview && src" to="body">
    <img
      v-if="hovering"
      :src="src"
      alt=""
      aria-hidden="true"
      class="pointer-events-none fixed z-50 object-contain drop-shadow-xl"
      :style="{ left: `${previewPosition.left}px`, top: `${previewPosition.top}px`, width: `${PREVIEW_WIDTH}px`, maxHeight: `${PREVIEW_HEIGHT}px` }"
    >
  </Teleport>
</template>
