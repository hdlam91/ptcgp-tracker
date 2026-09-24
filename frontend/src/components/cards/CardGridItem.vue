<script setup lang="ts">
import { Heart, Minus, Plus, Repeat } from '@lucide/vue'
import { computed } from 'vue'
import { Button } from '@/components/ui/button'
import { useLocalImage } from '@/composables/useLocalImage'
import { cn } from '@/lib/utils'
import type { CardCatalogEntry } from '@/types/catalog'

const props = withDefaults(defineProps<{
  card: CardCatalogEntry
  ownedCount: number
  wanted: boolean
  offered: boolean
  /** Hides the owned-count stepper and want/offer toggles, e.g. on a shared trade-list page. */
  readonly?: boolean
}>(), {
  readonly: false,
})

const emit = defineEmits<{
  (e: 'update:ownedCount', count: number): void
  (e: 'toggle-want'): void
  (e: 'toggle-offer'): void
}>()

const remoteImageUrl = computed(() => props.card.image ?? props.card.image_png)
const localImageUrl = computed(() => {
  if (!remoteImageUrl.value) return undefined
  const ext = remoteImageUrl.value.slice(remoteImageUrl.value.lastIndexOf('.'))
  return `/card-images/${props.card.id}${ext}`
})
const { src: imageUrl, onError: onImageError } = useLocalImage(localImageUrl.value, remoteImageUrl.value)

function increment() {
  emit('update:ownedCount', props.ownedCount + 1)
}

function decrement() {
  if (props.ownedCount > 0) {
    emit('update:ownedCount', props.ownedCount - 1)
  }
}
</script>

<template>
  <div
    :class="cn(
      'flex flex-col overflow-hidden rounded-lg border bg-card transition-opacity',
      !readonly && ownedCount === 0 && 'opacity-60',
    )"
  >
    <div
      :class="cn('relative aspect-[5/7] bg-muted', !readonly && 'cursor-pointer select-none')"
      :title="readonly ? undefined : 'Click to add one, right-click to remove one'"
      @click="!readonly && increment()"
      @contextmenu.prevent="!readonly && decrement()"
    >
      <img
        v-if="imageUrl"
        :src="imageUrl"
        :alt="card.name"
        class="h-full w-full object-contain"
        loading="lazy"
        @error="onImageError"
      >
      <span
        v-if="ownedCount > 0"
        class="absolute right-1.5 top-1.5 rounded-full bg-primary px-2 py-0.5 text-xs font-semibold text-primary-foreground"
      >
        ×{{ ownedCount }}
      </span>
    </div>

    <div class="flex flex-1 flex-col gap-2 p-2.5">
      <div>
        <p class="truncate text-sm font-medium" :title="card.name">
          {{ card.name }}
        </p>
        <p class="text-xs text-muted-foreground">
          {{ card.id }} · {{ card.rarity }}
        </p>
      </div>

      <div v-if="!readonly" class="mt-auto flex items-center justify-between gap-2">
        <div class="flex items-center gap-1">
          <Button variant="outline" size="icon" class="h-7 w-7" :disabled="ownedCount === 0" @click="decrement">
            <Minus class="size-3.5" />
          </Button>
          <span class="w-4 text-center text-sm tabular-nums">{{ ownedCount }}</span>
          <Button variant="outline" size="icon" class="h-7 w-7" @click="increment">
            <Plus class="size-3.5" />
          </Button>
        </div>

        <div v-if="card.tradable" class="flex items-center gap-1">
          <Button
            :variant="wanted ? 'default' : 'outline'"
            size="icon"
            class="h-7 w-7"
            title="Want this card"
            @click="emit('toggle-want')"
          >
            <Heart class="size-3.5" />
          </Button>
          <Button
            :variant="offered ? 'default' : 'outline'"
            size="icon"
            class="h-7 w-7"
            title="Offer this card for trade"
            @click="emit('toggle-offer')"
          >
            <Repeat class="size-3.5" />
          </Button>
        </div>
      </div>
    </div>
  </div>
</template>
