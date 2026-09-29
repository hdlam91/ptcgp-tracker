<script setup lang="ts">
import type { RarityFilter } from '@/composables/useCardFilters'
import { Crown, Diamond, Sparkles, Star } from '@lucide/vue'
import { computed } from 'vue'

const props = defineProps<{
  rarity: RarityFilter
}>()

const DIAMOND_TIERS: Record<string, number> = { '◊': 1, '◊◊': 2, '◊◊◊': 3, '◊◊◊◊': 4 }
const STAR_TIERS: Record<string, number> = { '☆': 1, '☆☆': 2, '☆☆☆': 3 }

// Repeats the tier icon the same number of times the game itself does (e.g. ◊◊◊ is three
// diamonds), rather than spelling the tier out as text. Each tier gets the color it does in
// the game: silvery diamonds, gold stars, a glowing gold crown, and a glowing purple sparkle
// for shiny.
const TONE_CLASS = {
  diamond: 'text-slate-400 dark:text-slate-300',
  star: 'text-amber-500 dark:text-amber-400',
  shiny: 'text-purple-500 dark:text-purple-400 drop-shadow-[0_0_4px_rgba(168,85,247,0.85)]',
  crown: 'text-amber-500 dark:text-amber-400 drop-shadow-[0_0_4px_rgba(245,158,11,0.85)]',
} as const

const parsed = computed(() => {
  if (props.rarity === 'Crown Rare')
    return { icon: Crown, count: 1, tone: 'crown' as const }
  if (props.rarity === 'Promo')
    return null
  if (props.rarity.endsWith(' Shiny')) {
    const base = props.rarity.slice(0, -' Shiny'.length)
    return { icon: Sparkles, count: STAR_TIERS[base] ?? 1, tone: 'shiny' as const }
  }
  if (props.rarity in DIAMOND_TIERS)
    return { icon: Diamond, count: DIAMOND_TIERS[props.rarity], tone: 'diamond' as const }
  return { icon: Star, count: STAR_TIERS[props.rarity] ?? 1, tone: 'star' as const }
})
</script>

<template>
  <span v-if="parsed" class="inline-flex items-center gap-0.5">
    <component
      :is="parsed.icon"
      v-for="n in parsed.count"
      :key="n"
      class="size-3.5"
      :class="TONE_CLASS[parsed.tone]"
      fill="currentColor"
    />
  </span>
  <span v-else class="text-xs font-medium">Promo</span>
</template>
