import { computed, ref } from 'vue'
import { collectionService } from '@/services/collectionService'
import type { SetSummaryResponse } from '@/types/api'

// Module-scoped singleton so every view (the sets overview and each set's card
// grid) shares one up-to-date picture of what the user owns, without a state
// management library.
const ownedCounts = ref<Map<string, number>>(new Map())
const loaded = ref(false)

// Guards against out-of-order responses: if the user clicks +/- rapidly, an
// older PUT could resolve after a newer one and must not clobber it.
const latestRequestSeq = new Map<string, number>()

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
  // Apply optimistically first, synchronously, so a rapid second click reads
  // this value (via the ownedCount prop) instead of racing the still-in-flight
  // request for the first click and recomputing "current + 1" from a stale 0.
  const optimistic = new Map(ownedCounts.value)
  if (ownedCount === 0) {
    optimistic.delete(cardId)
  }
  else {
    optimistic.set(cardId, ownedCount)
  }
  ownedCounts.value = optimistic

  const mySeq = (latestRequestSeq.get(cardId) ?? 0) + 1
  latestRequestSeq.set(cardId, mySeq)

  const result = await collectionService.setOwnedCount(cardId, ownedCount)

  // A newer request for this card has since been issued — let its own
  // resolution be the one that reconciles state.
  if (latestRequestSeq.get(cardId) !== mySeq) {
    return
  }

  const confirmed = new Map(ownedCounts.value)
  if (result.ownedCount === 0) {
    confirmed.delete(cardId)
  }
  else {
    confirmed.set(cardId, result.ownedCount)
  }
  ownedCounts.value = confirmed
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
