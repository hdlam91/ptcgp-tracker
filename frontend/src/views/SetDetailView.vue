<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import CardFilterBar from '@/components/cards/CardFilterBar.vue'
import CardGrid from '@/components/cards/CardGrid.vue'
import RarityProgressPanel from '@/components/cards/RarityProgressPanel.vue'
import { useCardCatalog } from '@/composables/useCardCatalog'
import { useCardFilters } from '@/composables/useCardFilters'
import { useCollection } from '@/composables/useCollection'
import { useRarityGroups } from '@/composables/useRarityGroups'
import { useTradeList } from '@/composables/useTradeList'

const route = useRoute()
const setCode = computed(() => route.params.setCode as string)

const { getCardsBySet, getExpansion } = useCardCatalog()
const { ownedCounts, getOwnedCount, setOwnedCount, ensureLoaded: ensureCollectionLoaded } = useCollection()
const { wantedCardIds, offeredCardIds, toggle, ensureLoaded: ensureTradeListLoaded } = useTradeList()

const cards = computed(() => getCardsBySet(setCode.value))
const expansion = computed(() => getExpansion(setCode.value))

const {
  search, setCode: setCodeFilter, rarity, pack, ownership,
  setOptions, rarityOptions, packOptions, filteredCards, hasActiveFilters, resetFilters,
} = useCardFilters(cards, getOwnedCount)

const { groups: rarityGroups } = useRarityGroups(cards, getOwnedCount)

const completingKey = ref<string | null>(null)

async function completeGroup(key: string) {
  const group = rarityGroups.value.find(g => g.key === key)
  if (!group || group.missingCardIds.length === 0) return

  completingKey.value = key
  try {
    // Chunked rather than one giant Promise.all — a set can have 200+ cards in a
    // rarity group, and firing that many PUTs at once in one burst is needless
    // load for no visible benefit over a handful of quick batches.
    const batchSize = 8
    for (let i = 0; i < group.missingCardIds.length; i += batchSize) {
      const batch = group.missingCardIds.slice(i, i + batchSize)
      await Promise.all(batch.map(cardId => setOwnedCount(cardId, 1)))
    }
  }
  finally {
    completingKey.value = null
  }
}

onMounted(async () => {
  await Promise.all([ensureCollectionLoaded(), ensureTradeListLoaded()])
})
</script>

<template>
  <div class="mx-auto max-w-6xl p-4 sm:p-6">
    <RouterLink to="/" class="text-sm text-muted-foreground hover:underline">
      ← All sets
    </RouterLink>
    <h1 class="mt-1 text-2xl font-semibold">
      {{ expansion?.name ?? setCode }}
    </h1>

    <RarityProgressPanel
      v-if="rarityGroups.length > 0"
      class="mt-4"
      :groups="rarityGroups"
      :completing-key="completingKey"
      @complete="completeGroup"
    />

    <CardFilterBar
      v-model:search="search"
      v-model:set-code="setCodeFilter"
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
      @reset="resetFilters"
    />

    <p v-if="filteredCards.length === 0" class="mt-8 text-sm text-muted-foreground">
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
