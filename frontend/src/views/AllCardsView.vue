<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import CardFilterBar from '@/components/cards/CardFilterBar.vue'
import CardGrid from '@/components/cards/CardGrid.vue'
import { useCardCatalog } from '@/composables/useCardCatalog'
import { useCardFilters } from '@/composables/useCardFilters'
import { useCollection } from '@/composables/useCollection'
import { useTradeList } from '@/composables/useTradeList'

const { t } = useI18n()
const { getAllCards } = useCardCatalog()
const { ownedCounts, getOwnedCount, setOwnedCount, ensureLoaded: ensureCollectionLoaded } = useCollection()
const { wantedCardIds, offeredCardIds, toggle, ensureLoaded: ensureTradeListLoaded } = useTradeList()

const cards = computed(() => getAllCards())

const {
  search,
  setCode,
  rarity,
  pack,
  ownership,
  cardType,
  pokemonType,
  evolution,
  ability,
  moveType,
  advancedOptions,
  setOptions,
  rarityOptions,
  packOptions,
  filteredCards,
  hasActiveFilters,
  resetFilters,
} = useCardFilters(cards, getOwnedCount)

onMounted(async () => {
  await Promise.all([ensureCollectionLoaded(), ensureTradeListLoaded()])
})
</script>

<template>
  <div class="mx-auto max-w-6xl p-4 sm:p-6">
    <h1 class="text-2xl font-semibold">
      {{ t('views.allCards.title') }}
    </h1>
    <p class="mt-1 text-muted-foreground">
      {{ t('views.allCards.subtitle') }}
    </p>

    <CardFilterBar
      v-model:search="search"
      v-model:set-code="setCode"
      v-model:rarity="rarity"
      v-model:pack="pack"
      v-model:ownership="ownership"
      v-model:card-type="cardType"
      v-model:pokemon-type="pokemonType"
      v-model:evolution="evolution"
      v-model:ability="ability"
      v-model:move-type="moveType"
      :advanced-options="advancedOptions"
      class="mt-4"
      :set-options="setOptions"
      :rarity-options="rarityOptions"
      :pack-options="packOptions"
      :result-count="filteredCards.length"
      :total-count="cards.length"
      :has-active-filters="hasActiveFilters"
      set-selector="dropdown"
      @reset="resetFilters"
    />

    <p v-if="!hasActiveFilters" class="mt-10 rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">
      {{ t('views.allCards.emptyState', { count: cards.length }) }}
    </p>
    <p v-else-if="filteredCards.length === 0" class="mt-8 text-sm text-muted-foreground">
      {{ t('views.allCards.noResults') }}
    </p>
    <div v-else class="mt-6">
      <CardGrid
        :cards="filteredCards"
        :owned-counts="ownedCounts"
        :wanted-card-ids="wantedCardIds"
        :offered-card-ids="offeredCardIds"
        @update:owned-count="(cardId, count) => setOwnedCount(cardId, count)"
        @toggle-want="(cardId) => toggle(cardId, 'Want')"
        @toggle-offer="(cardId) => toggle(cardId, 'Offer')"
      />
    </div>
  </div>
</template>
