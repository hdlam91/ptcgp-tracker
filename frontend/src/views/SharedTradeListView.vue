<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import CardGrid from '@/components/cards/CardGrid.vue'
import { Button } from '@/components/ui/button'
import { useCardCatalog } from '@/composables/useCardCatalog'
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
      <div v-else class="mt-6">
        <CardGrid
          :cards="activeCards"
          :owned-counts="emptyMap"
          :wanted-card-ids="emptySet"
          :offered-card-ids="emptySet"
          readonly
        />
      </div>
    </template>
  </div>
</template>
