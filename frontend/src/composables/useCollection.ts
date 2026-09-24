import { computed, ref } from 'vue'
import { collectionService } from '@/services/collectionService'
import type { SetSummaryResponse } from '@/types/api'

// Module-scoped singleton so every view (the sets overview and each set's card
// grid) shares one up-to-date picture of what the user owns, without a state
// management library.
const ownedCounts = ref<Map<string, number>>(new Map())
const loaded = ref(false)

async function ensureLoaded() {
  if (loaded.value) return
  await reload()
}

async function reload() {
  const entries = await collectionService.list()
  ownedCounts.value = new Map(entries.map(e => [e.cardId, e.ownedCount]))
  loaded.value = true
}

async function setOwnedCount(cardId: string, ownedCount: number) {
  const result = await collectionService.setOwnedCount(cardId, ownedCount)
  const next = new Map(ownedCounts.value)
  if (result.ownedCount === 0) {
    next.delete(cardId)
  }
  else {
    next.set(cardId, result.ownedCount)
  }
  ownedCounts.value = next
}

export function useCollection() {
  return {
    ownedCounts: computed(() => ownedCounts.value),
    getOwnedCount: (cardId: string) => ownedCounts.value.get(cardId) ?? 0,
    ensureLoaded,
    reload,
    setOwnedCount,
    getSummary: (): Promise<SetSummaryResponse[]> => collectionService.summary(),
  }
}
