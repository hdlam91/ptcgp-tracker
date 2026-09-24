<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import CardFilterBar from '@/components/cards/CardFilterBar.vue'
import CardGrid from '@/components/cards/CardGrid.vue'
import TradeListShareCard from '@/components/trade/TradeListShareCard.vue'
import { Button } from '@/components/ui/button'
import { useCardCatalog } from '@/composables/useCardCatalog'
import { useCardFilters } from '@/composables/useCardFilters'
import { useCollection } from '@/composables/useCollection'
import { useTradeList } from '@/composables/useTradeList'
import type { TradeDirection } from '@/types/api'

const { getCard } = useCardCatalog()
const { ownedCounts, setOwnedCount, ensureLoaded: ensureCollectionLoaded } = useCollection()
const { wantedCardIds, offeredCardIds, toggle, ensureLoaded: ensureTradeListLoaded } = useTradeList()

const activeTab = ref<TradeDirection>('Want')

onMounted(async () => {
  await Promise.all([ensureCollectionLoaded(), ensureTradeListLoaded()])
})

const activeCardIds = computed(() => (activeTab.value === 'Want' ? wantedCardIds.value : offeredCardIds.value))
const activeCards = computed(() =>
  [...activeCardIds.value]
    .map(cardId => getCard(cardId))
    .filter(card => card !== undefined),
)

const {
  search, setCode, rarity, pack, ownership,
  setOptions, rarityOptions, packOptions, filteredCards, hasActiveFilters, resetFilters,
} = useCardFilters(activeCards)
</script>

<template>
  <div class="mx-auto max-w-6xl p-4 sm:p-6">
    <h1 class="text-2xl font-semibold">
      Trade list
    </h1>
    <p class="mt-1 text-muted-foreground">
      Cards you want, and cards you're willing to trade away.
    </p>

    <TradeListShareCard class="mt-4" />

    <div class="mt-6 flex gap-2">
      <Button :variant="activeTab === 'Want' ? 'default' : 'outline'" @click="activeTab = 'Want'">
        Want ({{ wantedCardIds.size }})
      </Button>
      <Button :variant="activeTab === 'Offer' ? 'default' : 'outline'" @click="activeTab = 'Offer'">
        Offer ({{ offeredCardIds.size }})
      </Button>
    </div>

    <p v-if="activeCards.length === 0" class="mt-8 text-sm text-muted-foreground">
      No cards here yet. Add some from a set's page.
    </p>

    <template v-else>
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
        :total-count="activeCards.length"
        :has-active-filters="hasActiveFilters"
        :show-ownership="false"
        @reset="resetFilters"
      />

      <p v-if="filteredCards.length === 0" class="mt-8 text-sm text-muted-foreground">
        No cards match these filters.
      </p>
    </template>

    <div v-if="activeCards.length > 0 && filteredCards.length > 0" class="mt-6">
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
