<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import CardGrid from '@/components/cards/CardGrid.vue'
import { useCardCatalog } from '@/composables/useCardCatalog'
import { useCollection } from '@/composables/useCollection'
import { useTradeList } from '@/composables/useTradeList'

const route = useRoute()
const setCode = computed(() => route.params.setCode as string)

const { getCardsBySet, getExpansion } = useCardCatalog()
const { ownedCounts, setOwnedCount, ensureLoaded: ensureCollectionLoaded } = useCollection()
const { wantedCardIds, offeredCardIds, toggle, ensureLoaded: ensureTradeListLoaded } = useTradeList()

const cards = computed(() => getCardsBySet(setCode.value))
const expansion = computed(() => getExpansion(setCode.value))

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

    <div class="mt-6">
      <CardGrid
        :cards="cards"
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
