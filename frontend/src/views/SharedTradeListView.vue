<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import CardGrid from '@/components/cards/CardGrid.vue'
import SetRarityFilter from '@/components/cards/SetRarityFilter.vue'
import { Button } from '@/components/ui/button'
import { useCardCatalog } from '@/composables/useCardCatalog'
import { useCardFilters } from '@/composables/useCardFilters'
import { getSharedTradeList } from '@/services/tradeListService'
import type { SharedTradeListResponse, TradeDirection } from '@/types/api'

const route = useRoute()
const { getCard } = useCardCatalog()

const shared = ref<SharedTradeListResponse | null>(null)
const notFound = ref(false)
const activeTab = ref<TradeDirection>('Want')

const emptySet = new Set<string>()
const emptyMap = new Map<string, number>()

onMounted(async () => {
  try {
    shared.value = await getSharedTradeList(route.params.handle as string)
  }
  catch {
    notFound.value = true
  }
})

const activeCards = computed(() => {
  if (!shared.value) return []
  return shared.value.entries
    .filter(e => e.direction === activeTab.value)
    .map(e => getCard(e.cardId))
    .filter(card => card !== undefined)
})

// Set and rarity only. The composable's other filters (search, pack, energy...) stay unused.
const { setCode, rarity, setOptions, rarityOptions, filteredCards, hasActiveFilters, resetFilters } = useCardFilters(activeCards)

// A set or rarity picked on one tab may not exist on the other, so start each tab unfiltered.
watch(activeTab, resetFilters)

// A shared list can be huge (e.g. everything missing); rendering thousands of cards at once
// visibly lags, so show a page at a time, same as your own trade list.
const PAGE_SIZE = 60
const visibleCount = ref(PAGE_SIZE)
watch([setCode, rarity, activeTab], () => { visibleCount.value = PAGE_SIZE })
const visibleCards = computed(() => filteredCards.value.slice(0, visibleCount.value))

const wantedCount = computed(() => shared.value?.entries.filter(e => e.direction === 'Want').length ?? 0)
const offeredCount = computed(() => shared.value?.entries.filter(e => e.direction === 'Offer').length ?? 0)
</script>

<template>
  <div class="mx-auto max-w-6xl p-4 sm:p-6">
    <p v-if="notFound" class="mt-8 text-sm text-muted-foreground">
      This share link is invalid or no longer active.
    </p>

    <template v-else-if="shared">
      <h1 class="text-2xl font-semibold">
        {{ shared.displayName }}'s trade list
      </h1>
      <p class="mt-1 text-muted-foreground">
        Read-only — get in touch with {{ shared.displayName }} to arrange a trade.
      </p>

      <div class="mt-4 flex gap-2">
        <Button :variant="activeTab === 'Want' ? 'default' : 'outline'" @click="activeTab = 'Want'">
          Wants ({{ wantedCount }})
        </Button>
        <Button :variant="activeTab === 'Offer' ? 'default' : 'outline'" @click="activeTab = 'Offer'">
          Offers ({{ offeredCount }})
        </Button>
      </div>

      <p v-if="activeCards.length === 0" class="mt-8 text-sm text-muted-foreground">
        Nothing here yet.
      </p>
      <template v-else>
        <SetRarityFilter
          v-model:set-code="setCode"
          v-model:rarity="rarity"
          class="mt-4"
          :set-options="setOptions"
          :rarity-options="rarityOptions"
          :result-count="filteredCards.length"
          :total-count="activeCards.length"
          :has-active-filters="hasActiveFilters"
          @reset="resetFilters"
        />

        <p v-if="filteredCards.length === 0" class="mt-8 text-sm text-muted-foreground">
          No cards match these filters.
        </p>
        <template v-else>
          <div class="mt-6">
            <CardGrid
              :cards="visibleCards"
              :owned-counts="emptyMap"
              :wanted-card-ids="emptySet"
              :offered-card-ids="emptySet"
              readonly
            />
          </div>
          <div v-if="visibleCards.length < filteredCards.length" class="mt-4 flex justify-center">
            <Button variant="outline" @click="visibleCount += PAGE_SIZE">
              Show more ({{ visibleCards.length }} / {{ filteredCards.length }})
            </Button>
          </div>
        </template>
      </template>
    </template>
  </div>
</template>
