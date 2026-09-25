<script setup lang="ts">
import { X } from '@lucide/vue'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import type { CardCatalogEntry, ExpansionEntry } from '@/types/catalog'

// Just set and rarity — the lean filter for read-only views like the shared trade list,
// where CardFilterBar's search, pack, ownership and metadata filters would be noise.
defineProps<{
  setOptions: ExpansionEntry[]
  rarityOptions: CardCatalogEntry['rarity'][]
  resultCount: number
  totalCount: number
  hasActiveFilters: boolean
}>()

const emit = defineEmits<{ (e: 'reset'): void }>()

const setCode = defineModel<string>('setCode', { required: true })
const rarity = defineModel<CardCatalogEntry['rarity'] | ''>('rarity', { required: true })

const selectClass = 'h-10 rounded-md border border-input bg-background px-3 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2'
</script>

<template>
  <div class="flex flex-wrap items-center gap-2">
    <select v-model="setCode" :class="selectClass" aria-label="Filter by set">
      <option value="">
        Any set
      </option>
      <option v-for="option in setOptions" :key="option.id" :value="option.id">
        {{ option.name }}
      </option>
    </select>

    <select v-model="rarity" :class="selectClass" aria-label="Filter by rarity">
      <option value="">
        Any rarity
      </option>
      <option v-for="option in rarityOptions" :key="option" :value="option">
        {{ option }}
      </option>
    </select>

    <Button v-if="hasActiveFilters" variant="ghost" size="sm" class="text-muted-foreground" @click="emit('reset')">
      <X class="size-3.5" />
      Clear
    </Button>

    <span :class="cn('ml-auto text-sm text-muted-foreground', hasActiveFilters && 'font-medium text-foreground')">
      {{ resultCount }} / {{ totalCount }} cards
    </span>
  </div>
</template>
