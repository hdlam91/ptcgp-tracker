import { computed, ref } from 'vue'
import { tradeListService } from '@/services/tradeListService'
import type { TradeDirection } from '@/types/api'

// Module-scoped singleton, same reasoning as useCollection: the per-card toggle
// buttons (in a set's card grid) and the dedicated trade-list view need to agree
// on state without lifting it through props.
const wanted = ref<Set<string>>(new Set())
const offered = ref<Set<string>>(new Set())
const loaded = ref(false)

function setFor(direction: TradeDirection) {
  return direction === 'Want' ? wanted : offered
}

async function ensureLoaded() {
  if (loaded.value) return
  await reload()
}

async function reload() {
  const [wantedEntries, offeredEntries] = await Promise.all([
    tradeListService.list('Want'),
    tradeListService.list('Offer'),
  ])
  wanted.value = new Set(wantedEntries.map(e => e.cardId))
  offered.value = new Set(offeredEntries.map(e => e.cardId))
  loaded.value = true
}

async function add(cardId: string, direction: TradeDirection) {
  // Optimistic: a rapid second click must see this card as already added,
  // not race the still-in-flight first request.
  const target = setFor(direction)
  if (target.value.has(cardId)) return
  target.value = new Set(target.value).add(cardId)

  try {
    await tradeListService.add(cardId, direction)
  }
  catch (error) {
    target.value = new Set([...target.value].filter(id => id !== cardId))
    throw error
  }
}

async function remove(cardId: string, direction: TradeDirection) {
  const target = setFor(direction)
  if (!target.value.has(cardId)) return
  const next = new Set(target.value)
  next.delete(cardId)
  target.value = next

  try {
    await tradeListService.remove(cardId, direction)
  }
  catch (error) {
    target.value = new Set(target.value).add(cardId)
    throw error
  }
}

async function toggle(cardId: string, direction: TradeDirection) {
  if (setFor(direction).value.has(cardId)) {
    await remove(cardId, direction)
  }
  else {
    await add(cardId, direction)
  }
}

export function useTradeList() {
  return {
    wantedCardIds: computed(() => wanted.value),
    offeredCardIds: computed(() => offered.value),
    isWanted: (cardId: string) => wanted.value.has(cardId),
    isOffered: (cardId: string) => offered.value.has(cardId),
    ensureLoaded,
    reload,
    add,
    remove,
    toggle,
  }
}
