<script setup lang="ts">
import type { SharedTradeListResponse, TradeDirection } from '@/types/api'
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import CardGrid from '@/components/cards/CardGrid.vue'
import SetRarityFilter from '@/components/cards/SetRarityFilter.vue'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/composables/useAuth'
import { useCardCatalog } from '@/composables/useCardCatalog'
import { useCardFilters } from '@/composables/useCardFilters'
import { getSharedTradeList } from '@/services/tradeListService'

const { t } = useI18n()
const route = useRoute()
const { getCard } = useCardCatalog()
const { isAuthenticated } = useAuth()

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
  if (!shared.value)
    return []
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
watch([setCode, rarity, activeTab], () => {
  visibleCount.value = PAGE_SIZE
})
const visibleCards = computed(() => filteredCards.value.slice(0, visibleCount.value))

const wantedCount = computed(() => shared.value?.entries.filter(e => e.direction === 'Want').length ?? 0)
const offeredCount = computed(() => shared.value?.entries.filter(e => e.direction === 'Offer').length ?? 0)
</script>

<template>
  <div class="mx-auto max-w-6xl p-4 sm:p-6">
    <p v-if="notFound" class="mt-8 text-sm text-muted-foreground">
      {{ t('trade.sharedPage.notFound') }}
    </p>

    <template v-else-if="shared">
      <h1 class="text-2xl font-semibold">
        {{ t('trade.sharedPage.title', { displayName: shared.displayName }) }}
      </h1>
      <p class="mt-1 text-muted-foreground">
        {{ t('trade.sharedPage.readOnlyNotice', { displayName: shared.displayName }) }}
      </p>

      <div class="mt-4 flex gap-2">
        <Button :variant="activeTab === 'Want' ? 'default' : 'outline'" @click="activeTab = 'Want'">
          {{ t('trade.sharedPage.wantsCount', { count: wantedCount }) }}
        </Button>
        <Button :variant="activeTab === 'Offer' ? 'default' : 'outline'" @click="activeTab = 'Offer'">
          {{ t('trade.sharedPage.offersCount', { count: offeredCount }) }}
        </Button>
      </div>

      <p v-if="activeCards.length === 0" class="mt-8 text-sm text-muted-foreground">
        {{ t('trade.sharedPage.emptyActiveCards') }}
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
          {{ t('trade.sharedPage.noFilterMatches') }}
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
              {{ t('trade.sharedPage.showMore', { visible: visibleCards.length, total: filteredCards.length }) }}
            </Button>
          </div>
        </template>
      </template>
    </template>

    <!-- Logged-in visitors get the app's own header instead. Everyone else would otherwise be stuck on this
         page (an installed app has no address bar or back button). -->
    <p v-if="!isAuthenticated && (shared || notFound)" class="mb-10 mt-10 text-center text-sm text-muted-foreground">
      {{ t('trade.sharedPage.loginPrompt') }}
      <RouterLink to="/login" class="font-medium text-primary underline-offset-4 hover:underline">
        {{ t('trade.sharedPage.logIn') }}
      </RouterLink>
    </p>
  </div>
</template>
