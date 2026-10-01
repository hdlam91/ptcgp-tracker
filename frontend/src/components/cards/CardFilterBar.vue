<script setup lang="ts">
import type { OwnershipFilter, PackOption, RarityFilter } from '@/composables/useCardFilters'
import type { AbilityFilter, CardTypeFilter, EnergyType, EvolutionFilter, FilterOption } from '@/lib/cardMetadata'
import type { ExpansionEntry } from '@/types/catalog'
import { Search, SlidersHorizontal, X } from '@lucide/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import RaritySelect from '@/components/cards/RaritySelect.vue'
import SetTile from '@/components/cards/SetTile.vue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { cn } from '@/lib/utils'

const props = withDefaults(defineProps<{
  setOptions: ExpansionEntry[]
  rarityOptions: RarityFilter[]
  packOptions: PackOption[]
  advancedOptions: {
    cardTypes: FilterOption<CardTypeFilter>[]
    pokemonTypes: FilterOption<EnergyType>[]
    evolutions: FilterOption<EvolutionFilter>[]
    abilities: FilterOption<AbilityFilter>[]
    moveTypes: FilterOption<EnergyType>[]
  }
  resultCount: number
  totalCount: number
  hasActiveFilters: boolean
  /** Hidden on views with no ownership state, e.g. a shared read-only trade list. */
  showOwnership?: boolean
  /** The trade list can span every set at once with no natural pack art to show; a plain dropdown reads faster there than a wall of tiles. */
  setSelector?: 'tiles' | 'dropdown'
}>(), {
  showOwnership: true,
  setSelector: 'tiles',
})

const emit = defineEmits<{ (e: 'reset'): void }>()

const { t } = useI18n()

const search = defineModel<string>('search', { required: true })
const setCode = defineModel<string>('setCode', { required: true })
const rarity = defineModel<RarityFilter | ''>('rarity', { required: true })
const pack = defineModel<string>('pack', { required: true })
const ownership = defineModel<OwnershipFilter>('ownership', { required: true })
const cardType = defineModel<CardTypeFilter | ''>('cardType', { required: true })
const pokemonType = defineModel<EnergyType | ''>('pokemonType', { required: true })
const evolution = defineModel<EvolutionFilter | ''>('evolution', { required: true })
const ability = defineModel<AbilityFilter | ''>('ability', { required: true })
const moveType = defineModel<EnergyType | ''>('moveType', { required: true })

const hasAdvancedOptions = computed(() => Object.values(props.advancedOptions).some(options => options.length > 0))
const activeAdvancedCount = computed(() =>
  [cardType, pokemonType, evolution, ability, moveType].filter(filter => filter.value !== '').length)
// Start open when a filter is already applied, so it never hides why results are narrowed.
const showAdvanced = ref(activeAdvancedCount.value > 0)

const selectClass = 'h-10 rounded-md border border-input bg-background px-3 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2'
</script>

<template>
  <div class="flex flex-col gap-3">
    <div class="flex flex-wrap items-center gap-2">
      <div class="relative min-w-[10rem] flex-1 sm:max-w-xs">
        <Search class="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input v-model="search" type="text" :placeholder="t('cards.filterBar.searchPlaceholder')" class="pl-8" />
      </div>

      <select v-if="setSelector === 'dropdown' && setOptions.length > 1" v-model="setCode" :class="selectClass" :aria-label="t('cards.filterBar.filterBySet')">
        <option value="">
          {{ t('cards.filterBar.anySet') }}
        </option>
        <option v-for="option in setOptions" :key="option.id" :value="option.id">
          {{ option.name }}
        </option>
      </select>

      <RaritySelect v-model="rarity" :options="rarityOptions" />

      <select v-if="packOptions.length > 1" v-model="pack" :class="selectClass" :aria-label="t('cards.filterBar.filterByPack')">
        <option value="">
          {{ t('cards.filterBar.anyPack') }}
        </option>
        <option v-for="option in packOptions" :key="option.id" :value="option.name">
          {{ option.name }}
        </option>
      </select>

      <Button
        v-if="hasAdvancedOptions"
        :variant="activeAdvancedCount > 0 ? 'default' : 'outline'"
        size="sm"
        class="h-10"
        :aria-expanded="showAdvanced"
        aria-controls="advanced-filters"
        @click="showAdvanced = !showAdvanced"
      >
        <SlidersHorizontal class="size-3.5" />
        {{ t('cards.filterBar.filtersButton') }}
        <span v-if="activeAdvancedCount > 0" class="rounded-full bg-primary-foreground px-1.5 text-xs font-semibold text-primary">{{ activeAdvancedCount }}</span>
      </Button>

      <div v-if="showOwnership" class="flex items-center gap-1">
        <Button :variant="ownership === 'all' ? 'default' : 'outline'" size="sm" @click="ownership = 'all'">
          {{ t('cards.filterBar.ownershipAll') }}
        </Button>
        <Button :variant="ownership === 'owned' ? 'default' : 'outline'" size="sm" @click="ownership = 'owned'">
          {{ t('cards.filterBar.ownershipOwned') }}
        </Button>
        <Button :variant="ownership === 'missing' ? 'default' : 'outline'" size="sm" @click="ownership = 'missing'">
          {{ t('cards.filterBar.ownershipMissing') }}
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
        {{ t('cards.filterBar.clear') }}
      </Button>

      <span :class="cn('ml-auto text-sm text-muted-foreground', hasActiveFilters && 'font-medium text-foreground')">
        {{ t('cards.filterBar.resultCount', { count: resultCount, total: totalCount }) }}
      </span>
    </div>

    <div
      v-if="hasAdvancedOptions && showAdvanced"
      id="advanced-filters"
      class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5"
    >
      <div v-if="advancedOptions.cardTypes.length > 0" class="flex flex-col gap-1">
        <span class="text-xs text-muted-foreground" aria-hidden="true">{{ t('cards.filterBar.cardTypeLabel') }}</span>
        <select v-model="cardType" :class="selectClass" :aria-label="t('cards.filterBar.filterByCardType')">
          <option value="">
            {{ t('cards.filterBar.any') }}
          </option>
          <option v-for="option in advancedOptions.cardTypes" :key="option.value" :value="option.value">
            {{ option.label }}
          </option>
        </select>
      </div>
      <div v-if="advancedOptions.pokemonTypes.length > 0" class="flex flex-col gap-1">
        <span class="text-xs text-muted-foreground" aria-hidden="true">{{ t('cards.filterBar.pokemonTypeLabel') }}</span>
        <select v-model="pokemonType" :class="selectClass" :aria-label="t('cards.filterBar.filterByPokemonType')">
          <option value="">
            {{ t('cards.filterBar.any') }}
          </option>
          <option v-for="option in advancedOptions.pokemonTypes" :key="option.value" :value="option.value">
            {{ option.label }}
          </option>
        </select>
      </div>
      <div v-if="advancedOptions.evolutions.length > 0" class="flex flex-col gap-1">
        <span class="text-xs text-muted-foreground" aria-hidden="true">{{ t('cards.filterBar.evolutionLabel') }}</span>
        <select v-model="evolution" :class="selectClass" :aria-label="t('cards.filterBar.filterByEvolution')">
          <option value="">
            {{ t('cards.filterBar.any') }}
          </option>
          <option v-for="option in advancedOptions.evolutions" :key="option.value" :value="option.value">
            {{ option.label }}
          </option>
        </select>
      </div>
      <div v-if="advancedOptions.abilities.length > 0" class="flex flex-col gap-1">
        <span class="text-xs text-muted-foreground" aria-hidden="true">{{ t('cards.filterBar.abilityLabel') }}</span>
        <select v-model="ability" :class="selectClass" :aria-label="t('cards.filterBar.filterByAbility')">
          <option value="">
            {{ t('cards.filterBar.any') }}
          </option>
          <option v-for="option in advancedOptions.abilities" :key="option.value" :value="option.value">
            {{ option.label }}
          </option>
        </select>
      </div>
      <div v-if="advancedOptions.moveTypes.length > 0" class="flex flex-col gap-1">
        <span class="text-xs text-muted-foreground" aria-hidden="true" :title="t('cards.filterBar.moveTypeTooltip')">{{ t('cards.filterBar.moveTypeLabel') }}</span>
        <select v-model="moveType" :class="selectClass" :aria-label="t('cards.filterBar.filterByMoveType')" :title="t('cards.filterBar.moveTypeTooltip')">
          <option value="">
            {{ t('cards.filterBar.any') }}
          </option>
          <option v-for="option in advancedOptions.moveTypes" :key="option.value" :value="option.value">
            {{ option.label }}
          </option>
        </select>
      </div>
    </div>

    <div v-if="setSelector === 'tiles' && setOptions.length > 1" class="flex items-center gap-2 overflow-x-auto pb-1" role="group" :aria-label="t('cards.filterBar.filterBySet')">
      <button
        type="button"
        :class="cn(
          'flex h-[68px] w-16 shrink-0 flex-col items-center justify-center gap-1 rounded-md border text-xs transition-colors hover:bg-accent',
          setCode === '' ? 'border-primary bg-accent' : 'border-input text-muted-foreground',
        )"
        @click="setCode = ''"
      >
        {{ t('cards.filterBar.allSets') }}
      </button>
      <SetTile
        v-for="option in setOptions"
        :key="option.id"
        :set="option"
        :selected="setCode === option.id"
        @click="setCode = setCode === option.id ? '' : option.id"
      />
    </div>
  </div>
</template>
