<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import PackThumbnail from '@/components/cards/PackThumbnail.vue'
import SetProgressBar from '@/components/cards/SetProgressBar.vue'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { useCardCatalog } from '@/composables/useCardCatalog'
import { useCollection } from '@/composables/useCollection'
import type { SetSummaryResponse } from '@/types/api'

const { getExpansions } = useCardCatalog()
const { getSummary } = useCollection()

const expansions = getExpansions()
const summaryBySet = ref<Map<string, SetSummaryResponse>>(new Map())
const loading = ref(true)

onMounted(async () => {
  const summary = await getSummary()
  summaryBySet.value = new Map(summary.map(s => [s.setCode, s]))
  loading.value = false
})

const ownedFor = computed(() => (setCode: string) => summaryBySet.value.get(setCode)?.ownedUniqueCards ?? 0)
</script>

<template>
  <div class="mx-auto max-w-5xl p-4 sm:p-6">
    <h1 class="text-2xl font-semibold">
      Your sets
    </h1>
    <p class="mt-1 text-muted-foreground">
      Pick a set to view and update the cards you own.
    </p>

    <div class="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <RouterLink v-for="expansion in expansions" :key="expansion.id" :to="`/sets/${expansion.id}`">
        <Card class="h-full transition-colors hover:bg-accent">
          <CardHeader>
            <div class="flex items-center justify-between gap-3">
              <CardTitle class="text-base">
                {{ expansion.name }}
              </CardTitle>
              <div v-if="expansion.packs.some(pack => pack.image)" class="flex shrink-0 -space-x-4">
                <PackThumbnail
                  v-for="pack in expansion.packs.filter(pack => pack.image)"
                  :key="pack.id"
                  :pack="pack"
                  class="h-12 w-12 shrink-0 rounded-md"
                />
              </div>
            </div>
          </CardHeader>
          <CardContent>
            <SetProgressBar :owned="loading ? 0 : ownedFor(expansion.id)" :total="expansion.total_cards" />
          </CardContent>
        </Card>
      </RouterLink>
    </div>
  </div>
</template>
