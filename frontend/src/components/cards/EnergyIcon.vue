<script setup lang="ts">
import { Cog, Droplet, Eye, Flame, HandFist, Leaf, Moon, Sparkles, Star, Zap } from '@lucide/vue'
import type { Component } from 'vue'
import { computed } from 'vue'
import type { EnergyType } from '@/lib/cardMetadata'
import { cn } from '@/lib/utils'

const props = withDefaults(defineProps<{
  type: EnergyType
  size?: 'sm' | 'md'
}>(), {
  size: 'md',
})

// Generic glyphs on a coloured disc, one per energy type. `solid` icons read better
// filled in; line-art ones (eye, fist, cog...) are left as outlines.
const STYLES: Record<EnergyType, { icon: Component, solid: boolean, classes: string }> = {
  Grass: { icon: Leaf, solid: false, classes: 'bg-green-500 text-white' },
  Fire: { icon: Flame, solid: true, classes: 'bg-red-500 text-white' },
  Water: { icon: Droplet, solid: true, classes: 'bg-blue-500 text-white' },
  Lightning: { icon: Zap, solid: true, classes: 'bg-yellow-400 text-yellow-950' },
  Psychic: { icon: Eye, solid: false, classes: 'bg-purple-500 text-white' },
  Fighting: { icon: HandFist, solid: false, classes: 'bg-orange-700 text-white' },
  Darkness: { icon: Moon, solid: true, classes: 'bg-slate-800 text-white' },
  Metal: { icon: Cog, solid: false, classes: 'bg-slate-400 text-slate-950' },
  Dragon: { icon: Sparkles, solid: false, classes: 'bg-amber-600 text-white' },
  Colorless: { icon: Star, solid: true, classes: 'bg-zinc-200 text-zinc-700 dark:bg-zinc-300' },
}

const style = computed(() => STYLES[props.type])
</script>

<template>
  <span
    role="img"
    :aria-label="`${type} energy`"
    :title="type"
    :class="cn(
      'inline-flex shrink-0 select-none items-center justify-center rounded-full',
      size === 'sm' ? 'size-4' : 'size-6',
      style.classes,
    )"
  >
    <component
      :is="style.icon"
      aria-hidden="true"
      :class="size === 'sm' ? 'size-2.5' : 'size-3.5'"
      :fill="style.solid ? 'currentColor' : 'none'"
    />
  </span>
</template>
