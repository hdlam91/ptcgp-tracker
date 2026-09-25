<script setup lang="ts">
import { Crown, Diamond, Sparkles, Star } from '@lucide/vue'
import { cn } from '@/lib/utils'
import type { RarityGroup } from '@/composables/useRarityGroups'

defineProps<{
  groups: RarityGroup[]
}>()

const ICONS = { diamond: Diamond, star: Star, shiny: Sparkles, crown: Crown } as const
</script>

<template>
  <div class="flex flex-wrap gap-1.5">
    <span
      v-for="group in groups"
      :key="group.key"
      :class="cn(
        'inline-flex items-center gap-1 whitespace-nowrap rounded-full border px-2 py-0.5 text-xs font-medium',
        group.owned === group.total
          ? 'border-sky-300 bg-sky-100 text-sky-700 dark:border-sky-800 dark:bg-sky-950 dark:text-sky-300'
          : 'border-transparent bg-muted text-muted-foreground',
      )"
    >
      <component
        :is="ICONS[group.key as keyof typeof ICONS]"
        class="size-3"
        :fill="group.owned === group.total ? 'currentColor' : 'none'"
      />
      {{ group.owned }}/{{ group.total }}
    </span>
  </div>
</template>
