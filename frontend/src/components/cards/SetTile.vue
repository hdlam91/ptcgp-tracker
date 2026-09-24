<script setup lang="ts">
import PackThumbnail from '@/components/cards/PackThumbnail.vue'
import { cn } from '@/lib/utils'
import type { ExpansionEntry } from '@/types/catalog'

defineProps<{
  set: ExpansionEntry
  selected: boolean
}>()

defineEmits<{ (e: 'click'): void }>()
</script>

<template>
  <button
    type="button"
    :class="cn(
      'flex shrink-0 flex-col items-center gap-1 rounded-md border p-1.5 transition-colors hover:bg-accent',
      selected ? 'border-primary bg-accent' : 'border-input',
    )"
    :title="set.name"
    @click="$emit('click')"
  >
    <div v-if="set.packs.some(pack => pack.image)" class="flex -space-x-3">
      <PackThumbnail
        v-for="pack in set.packs.filter(pack => pack.image)"
        :key="pack.id"
        :pack="pack"
        class="h-10 w-10 rounded border-2 border-background bg-muted shadow-sm"
      />
    </div>
    <span class="max-w-24 truncate text-xs">{{ set.name }}</span>
  </button>
</template>
