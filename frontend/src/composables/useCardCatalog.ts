import cardRecords from 'pokemon-tcg-pocket-cards/v5/collection'
import expansionRecords from 'pokemon-tcg-pocket-cards/v5/expansions'
import type { CardCatalogEntry, ExpansionEntry } from '@/types/catalog'

const cardsById = new Map<string, CardCatalogEntry>(
  (cardRecords as CardCatalogEntry[]).map(card => [card.id, card]),
)

const expansionsByCode = new Map<string, ExpansionEntry>(
  (expansionRecords as ExpansionEntry[]).map(expansion => [expansion.id, expansion]),
)

const cardsBySetCode = new Map<string, CardCatalogEntry[]>()
for (const card of cardsById.values()) {
  const cards = cardsBySetCode.get(card.set_code) ?? []
  cards.push(card)
  cardsBySetCode.set(card.set_code, cards)
}

// Sets without a release_date (promos) sort last, not first.
const expansionsSortedByRelease = [...expansionsByCode.values()].sort((a, b) =>
  (a.release_date ?? '9999-99-99').localeCompare(b.release_date ?? '9999-99-99'))

// All cards, ordered by set release date then card id, so a global card list
// reads the same way the sets overview does rather than following whatever
// order the dataset's own JSON array happens to use.
const allCardsSorted = expansionsSortedByRelease.flatMap(expansion => cardsBySetCode.get(expansion.id) ?? [])

/**
 * Read-only access to the card dataset bundled via the pokemon-tcg-pocket-cards
 * npm package (pinned to the same tag the backend fetches). The BFF never returns
 * card metadata, so this is the frontend's only source for it.
 */
export function useCardCatalog() {
  return {
    getCard: (cardId: string): CardCatalogEntry | undefined => cardsById.get(cardId),
    getCardsBySet: (setCode: string): CardCatalogEntry[] => cardsBySetCode.get(setCode) ?? [],
    getAllCards: (): CardCatalogEntry[] => allCardsSorted,
    getExpansions: (): ExpansionEntry[] => expansionsSortedByRelease,
    getExpansion: (setCode: string): ExpansionEntry | undefined => expansionsByCode.get(setCode),
  }
}
