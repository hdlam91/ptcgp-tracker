<script setup lang="ts">
import { Heart, Info, Minus, Plus, Repeat } from '@lucide/vue'
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import { Button } from '@/components/ui/button'
import { useLocalImage } from '@/composables/useLocalImage'
import { cn } from '@/lib/utils'
import type { CardCatalogEntry } from '@/types/catalog'

const props = withDefaults(defineProps<{
  card: CardCatalogEntry
  ownedCount: number
  wanted: boolean
  offered: boolean
  /** Hides the +/- controls, want/offer toggles and the link to the card page, e.g. on a shared trade-list page. */
  readonly?: boolean
  /** Dims the card when unowned — off for lists where that's every card by default, e.g. a wishlist. */
  dimMissing?: boolean
}>(), {
  readonly: false,
  dimMissing: true,
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

// Translucent "glass" pills so they read on any card art, light or dark.
const glassButtonClass = 'pointer-events-auto size-10 rounded-full border border-white/40 bg-black/35 text-white shadow-lg backdrop-blur-md transition hover:bg-black/55 hover:text-white active:scale-95 disabled:opacity-40'

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
      'group flex flex-col overflow-hidden rounded-lg border bg-card transition-opacity',
      // Dimmed until you own it, but back to full strength under the pointer so the hover controls read clearly.
      !readonly && dimMissing && ownedCount === 0 && 'opacity-60 hover:opacity-100',
    )"
  >
    <div class="relative aspect-[5/7] bg-muted">
      <!-- The art opens the card's page. Read-only views (the public shared list) have no page to open. -->
      <component
        :is="readonly ? 'div' : RouterLink"
        v-bind="readonly ? {} : { to: `/cards/${card.id}` }"
        class="block h-full w-full"
      >
        <img
          v-if="imageUrl"
          :src="imageUrl"
          :alt="card.name"
          class="h-full w-full object-contain"
          loading="lazy"
          @error="onImageError"
        >
      </component>
      <span
        v-if="ownedCount > 0"
        class="pointer-events-none absolute right-1.5 top-1.5 rounded-full bg-primary px-2 py-0.5 text-xs font-semibold text-primary-foreground"
      >
        ×{{ ownedCount }}
      </span>

      <!-- Only appears on hover (or keyboard focus) where a pointer exists; always shown on touch screens,
           which can't hover. The wrapper ignores the pointer so the art underneath stays clickable. -->
      <div
        v-if="!readonly"
        class="pointer-events-none absolute inset-x-0 bottom-0 flex items-end justify-between bg-gradient-to-t from-black/25 to-transparent px-2.5 pb-2.5 pt-10 transition-opacity [@media(hover:hover)]:opacity-0 group-hover:opacity-100 group-focus-within:opacity-100"
      >
        <Button
          variant="ghost"
          size="icon"
          :class="glassButtonClass"
          :disabled="ownedCount === 0"
          aria-label="Remove one copy"
          title="Remove one"
          @click="decrement"
        >
          <Minus class="size-5" :stroke-width="2.5" />
        </Button>
        <Button
          variant="ghost"
          size="icon"
          :class="glassButtonClass"
          aria-label="Add one copy"
          title="Add one"
          @click="increment"
        >
          <Plus class="size-5" :stroke-width="2.5" />
        </Button>
      </div>
    </div>

    <div class="flex flex-1 flex-col gap-2 p-2.5">
      <div class="flex items-start justify-between gap-1">
        <div class="min-w-0">
          <p class="truncate text-sm font-medium" :title="card.name">
            {{ card.name }}
          </p>
          <p class="text-xs text-muted-foreground">
            {{ card.id }} · {{ card.rarity }}
          </p>
        </div>
        <!-- A link, not a button: it opens the card's own page and shouldn't count as a control. -->
        <RouterLink
          v-if="!readonly"
          :to="`/cards/${card.id}`"
          :aria-label="`Details for ${card.name}`"
          title="Card details"
          class="-mr-1 -mt-0.5 inline-flex size-7 shrink-0 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
        >
          <Info class="size-4" />
        </RouterLink>
      </div>

      <div v-if="!readonly" class="mt-auto flex items-center justify-between gap-2">
        <div class="flex items-center gap-1">
          <Button
            variant="outline"
            size="icon"
            class="h-7 w-7"
            :disabled="ownedCount === 0"
            aria-label="Decrease owned count"
            @click="decrement"
          >
            <Minus class="size-3.5" />
          </Button>
          <span class="w-4 text-center text-sm tabular-nums">{{ ownedCount }}</span>
          <Button variant="outline" size="icon" class="h-7 w-7" aria-label="Increase owned count" @click="increment">
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
