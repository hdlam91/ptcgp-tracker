<script setup lang="ts">
import { Search, X } from '@lucide/vue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import type { OwnershipFilter } from '@/composables/useCardFilters'
import { cn } from '@/lib/utils'
import type { CardCatalogEntry, ExpansionEntry } from '@/types/catalog'

withDefaults(defineProps<{
  setOptions: ExpansionEntry[]
  rarityOptions: CardCatalogEntry['rarity'][]
  packOptions: string[]
  resultCount: number
  totalCount: number
  hasActiveFilters: boolean
  /** Hidden on views with no ownership state, e.g. a shared read-only trade list. */
  showOwnership?: boolean
}>(), {
  showOwnership: true,
})

const emit = defineEmits<{ (e: 'reset'): void }>()

const search = defineModel<string>('search', { required: true })
const setCode = defineModel<string>('setCode', { required: true })
const rarity = defineModel<CardCatalogEntry['rarity'] | ''>('rarity', { required: true })
const pack = defineModel<string>('pack', { required: true })
const ownership = defineModel<OwnershipFilter>('ownership', { required: true })

const selectClass = 'h-10 rounded-md border border-input bg-background px-3 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2'
</script>

<template>
  <div class="flex flex-wrap items-center gap-2">
    <div class="relative min-w-[10rem] flex-1 sm:max-w-xs">
      <Search class="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
      <Input v-model="search" type="text" placeholder="Search by name or number" class="pl-8" />
    </div>

    <select v-if="setOptions.length > 1" v-model="setCode" :class="selectClass" aria-label="Filter by set">
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

    <select v-if="packOptions.length > 1" v-model="pack" :class="selectClass" aria-label="Filter by pack">
      <option value="">
        Any pack
      </option>
      <option v-for="option in packOptions" :key="option" :value="option">
        {{ option }}
      </option>
    </select>

    <div v-if="showOwnership" class="flex items-center gap-1">
      <Button :variant="ownership === 'all' ? 'default' : 'outline'" size="sm" @click="ownership = 'all'">
        All
      </Button>
      <Button :variant="ownership === 'owned' ? 'default' : 'outline'" size="sm" @click="ownership = 'owned'">
        Owned
      </Button>
      <Button :variant="ownership === 'missing' ? 'default' : 'outline'" size="sm" @click="ownership = 'missing'">
        Missing
      </Button>
    </div>

    <Button
      v-if="hasActiveFilters"
      variant="ghost"
      size="sm"
      class="text-muted-foreground"
      @click="emit('reset')"
    >
      <X class="size-3.5" />
      Clear
    </Button>

    <span :class="cn('ml-auto text-sm text-muted-foreground', hasActiveFilters && 'font-medium text-foreground')">
      {{ resultCount }} / {{ totalCount }} cards
    </span>
  </div>
</template>
