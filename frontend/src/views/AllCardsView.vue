<script setup lang="ts">
import { computed, onMounted } from 'vue'
import CardFilterBar from '@/components/cards/CardFilterBar.vue'
import CardGrid from '@/components/cards/CardGrid.vue'
import { useCardCatalog } from '@/composables/useCardCatalog'
import { useCardFilters } from '@/composables/useCardFilters'
import { useCollection } from '@/composables/useCollection'
import { useTradeList } from '@/composables/useTradeList'

const { getAllCards } = useCardCatalog()
const { ownedCounts, getOwnedCount, setOwnedCount, ensureLoaded: ensureCollectionLoaded } = useCollection()
const { wantedCardIds, offeredCardIds, toggle, ensureLoaded: ensureTradeListLoaded } = useTradeList()

const cards = computed(() => getAllCards())

const {
  search, setCode, rarity, pack, ownership,
  setOptions, rarityOptions, packOptions, filteredCards, hasActiveFilters, resetFilters,
} = useCardFilters(cards, getOwnedCount)

onMounted(async () => {
  await Promise.all([ensureCollectionLoaded(), ensureTradeListLoaded()])
})
</script>

<template>
  <div class="mx-auto max-w-6xl p-4 sm:p-6">
    <h1 class="text-2xl font-semibold">
      All cards
    </h1>
    <p class="mt-1 text-muted-foreground">
      Search and filter across every set at once.
    </p>

    <CardFilterBar
      v-model:search="search"
      v-model:set-code="setCode"
      v-model:rarity="rarity"
      v-model:pack="pack"
      v-model:ownership="ownership"
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
      {{ cards.length }} cards across every set. Search by name or number, or pick a set, rarity, or pack to browse them.
    </p>
    <p v-else-if="filteredCards.length === 0" class="mt-8 text-sm text-muted-foreground">
      No cards match these filters.
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
