<script setup lang="ts">
import type { RarityFilter } from '@/composables/useCardFilters'
import { ChevronDown } from '@lucide/vue'
import { useEventListener } from '@vueuse/core'
import { ref } from 'vue'
import RarityIcon from '@/components/cards/RarityIcon.vue'
import { rarityFilterLabel } from '@/composables/useCardFilters'

// A native <select> can't render an icon inside an <option>, so rarity gets its own
// button + listbox instead — the same tier icons (diamond/star/shiny/crown) shown
// elsewhere in the app (e.g. the collection page's rarity badges), rather than raw
// dataset symbols or an emoji standing in for them.
defineProps<{
  options: RarityFilter[]
}>()

const rarity = defineModel<RarityFilter | ''>({ required: true })

const open = ref(false)

function pick(value: RarityFilter | '') {
  rarity.value = value
  open.value = false
}

useEventListener(document, 'pointerdown', (event: PointerEvent) => {
  if (!open.value)
    return
  if ((event.target as Element | null)?.closest('#rarity-listbox, [aria-controls="rarity-listbox"]'))
    return
  open.value = false
})
useEventListener(document, 'keydown', (event: KeyboardEvent) => {
  if (event.key === 'Escape')
    open.value = false
})
</script>

<template>
  <div class="relative">
    <button
      type="button"
      class="flex h-10 min-w-[6.5rem] items-center justify-between gap-2 rounded-md border border-input bg-background px-3 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
      :aria-label="`Filter by rarity: ${rarityFilterLabel(rarity)}`"
      aria-haspopup="listbox"
      :aria-expanded="open"
      aria-controls="rarity-listbox"
      @click="open = !open"
    >
      <RarityIcon v-if="rarity !== ''" :rarity="rarity" />
      <span v-else class="text-muted-foreground">Any rarity</span>
      <ChevronDown class="size-4 shrink-0 text-muted-foreground" />
    </button>

    <ul
      v-if="open"
      id="rarity-listbox"
      role="listbox"
      aria-label="Rarity"
      class="absolute left-0 top-full z-30 mt-1 min-w-[8rem] rounded-md border bg-background py-1 shadow-md"
    >
      <li
        role="option"
        :aria-selected="rarity === ''"
        class="cursor-pointer px-3 py-2 text-sm text-muted-foreground hover:bg-accent"
        :class="rarity === '' && 'bg-accent'"
        @click="pick('')"
      >
        Any rarity
      </li>
      <li
        v-for="option in options"
        :key="option"
        role="option"
        :aria-label="rarityFilterLabel(option)"
        :aria-selected="rarity === option"
        class="flex cursor-pointer items-center px-3 py-2 hover:bg-accent"
        :class="rarity === option && 'bg-accent'"
        @click="pick(option)"
      >
        <RarityIcon :rarity="option" />
      </li>
    </ul>
  </div>
</template>
