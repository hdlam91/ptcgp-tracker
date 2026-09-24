<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import CardFilterBar from '@/components/cards/CardFilterBar.vue'
import CardGrid from '@/components/cards/CardGrid.vue'
import TradeListShareCard from '@/components/trade/TradeListShareCard.vue'
import { Button } from '@/components/ui/button'
import { useCardCatalog } from '@/composables/useCardCatalog'
import { useCardFilters } from '@/composables/useCardFilters'
import { useCollection } from '@/composables/useCollection'
import { useTradeList } from '@/composables/useTradeList'
import { runInBatches } from '@/lib/batch'
import type { TradeDirection } from '@/types/api'

const { getCard, getAllCards } = useCardCatalog()
const { getOwnedCount, ownedCounts, setOwnedCount, ensureLoaded: ensureCollectionLoaded } = useCollection()
const { wantedCardIds, offeredCardIds, toggle, add, remove, ensureLoaded: ensureTradeListLoaded } = useTradeList()

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

// Every tradable card not already owned and not already on the want list —
// only tradable cards can be wanted at all (see CardGridItem's own gating).
const missingCardIds = computed(() =>
  getAllCards()
    .filter(card => card.tradable && getOwnedCount(card.id) === 0 && !wantedCardIds.value.has(card.id))
    .map(card => card.id),
)

const addingAllMissing = ref(false)

async function addAllMissingToWishlist() {
  if (missingCardIds.value.length === 0) return
  addingAllMissing.value = true
  try {
    await runInBatches(missingCardIds.value, 8, cardId => add(cardId, 'Want'))
  }
  finally {
    addingAllMissing.value = false
  }
}

// Cards on the want list that are now owned — left behind once a wanted card
// gets pulled from a pack or bought, since owning a card doesn't auto-remove it.
const ownedWantedCardIds = computed(() =>
  [...wantedCardIds.value].filter(cardId => getOwnedCount(cardId) > 0),
)

const removingOwned = ref(false)

async function removeOwnedFromWishlist() {
  if (ownedWantedCardIds.value.length === 0) return
  removingOwned.value = true
  try {
    await runInBatches(ownedWantedCardIds.value, 8, cardId => remove(cardId, 'Want'))
  }
  finally {
    removingOwned.value = false
  }
}

const {
  search, setCode, rarity, pack, ownership,
  setOptions, rarityOptions, packOptions, filteredCards, hasActiveFilters, resetFilters,
} = useCardFilters(activeCards)

// The want list should show cards by default, unlike the "search everything"
// All Cards page — but a bulk-added list can run into the thousands, and
// rendering that many cards at once measurably lags (~5s render, ~800ms per
// click). Paginate instead of gating the whole grid behind a required filter.
const PAGE_SIZE = 60
const visibleCount = ref(PAGE_SIZE)
watch([search, setCode, rarity, pack, activeTab], () => { visibleCount.value = PAGE_SIZE })
const visibleCards = computed(() => filteredCards.value.slice(0, visibleCount.value))
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

    <div class="mt-6 flex flex-wrap items-center gap-2">
      <Button :variant="activeTab === 'Want' ? 'default' : 'outline'" @click="activeTab = 'Want'">
        Want ({{ wantedCardIds.size }})
      </Button>
      <Button :variant="activeTab === 'Offer' ? 'default' : 'outline'" @click="activeTab = 'Offer'">
        Offer ({{ offeredCardIds.size }})
      </Button>
      <Button
        v-if="activeTab === 'Want' && missingCardIds.length > 0"
        variant="outline"
        class="sm:ml-auto"
        :disabled="addingAllMissing"
        @click="addAllMissingToWishlist"
      >
        {{ addingAllMissing ? 'Adding…' : `Add all missing to wishlist (${missingCardIds.length})` }}
      </Button>
      <Button
        v-if="activeTab === 'Want' && ownedWantedCardIds.length > 0"
        variant="outline"
        :class="missingCardIds.length > 0 ? '' : 'sm:ml-auto'"
        :disabled="removingOwned"
        @click="removeOwnedFromWishlist"
      >
        {{ removingOwned ? 'Removing…' : `Remove cards I already own (${ownedWantedCardIds.length})` }}
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
        set-selector="dropdown"
        @reset="resetFilters"
      />

      <p v-if="filteredCards.length === 0" class="mt-8 text-sm text-muted-foreground">
        No cards match these filters.
      </p>
      <template v-else>
        <div class="mt-6">
          <CardGrid
            :cards="visibleCards"
            :owned-counts="ownedCounts"
            :wanted-card-ids="wantedCardIds"
            :offered-card-ids="offeredCardIds"
            :dim-missing="activeTab === 'Offer'"
            @update:owned-count="(cardId, count) => setOwnedCount(cardId, count)"
            @toggle-want="(cardId) => toggle(cardId, 'Want')"
            @toggle-offer="(cardId) => toggle(cardId, 'Offer')"
          />
        </div>
        <div v-if="visibleCards.length < filteredCards.length" class="mt-4 flex justify-center">
          <Button variant="outline" @click="visibleCount += PAGE_SIZE">
            Show more ({{ visibleCards.length }} / {{ filteredCards.length }})
          </Button>
        </div>
      </template>
    </template>
  </div>
</template>
