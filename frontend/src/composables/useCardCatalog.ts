import cardRecords from 'pokemon-tcg-pocket-cards/v5/collection'
import expansionRecords from 'pokemon-tcg-pocket-cards/v5/expansions'
import gameplayRecords from 'pokemon-tcg-pocket-cards/v5/gameplay/no-image'
import { type CardMeta, deriveCardMeta, printCardId } from '@/lib/cardMetadata'
import type { CardCatalogEntry, ExpansionEntry, GameplayEntry } from '@/types/catalog'

const cardsById = new Map<string, CardCatalogEntry>(
  (cardRecords as CardCatalogEntry[]).map(card => [card.id, card]),
)

const expansionsByCode = new Map<string, ExpansionEntry>(
  (expansionRecords as ExpansionEntry[]).map(expansion => [expansion.id, expansion]),
)

// The dataset leaves every promo pack's image null (there's no real box art for
// a promo drop), but the source repo does host art for a couple of individual
// promo waves under the same images/webp/packs path used for real packs — it's
// just not wired up in expansions.json. Borrow one confirmed-good image per
// promo set as its representative icon, so Promo-A/B aren't the only sets with
// no art at all on the collection page and set picker.
// `targetPackId` is the pack actually declared in expansions.json to patch;
// `imageId` is whichever image file we know exists in the repo (they only
// happen to match for pa — pb's only declared pack is "pb-booster", which has
// no image file of its own, so it borrows pb-promov1's art instead).
const PROMO_PACK_IMAGE_OVERRIDES: Record<string, { targetPackId: string, imageId: string }> = {
  pa: { targetPackId: 'pa-promov1', imageId: 'pa-promov1' },
  pb: { targetPackId: 'pb-booster', imageId: 'pb-promov1' },
}

for (const [setCode, override] of Object.entries(PROMO_PACK_IMAGE_OVERRIDES)) {
  const expansion = expansionsByCode.get(setCode)
  const pack = expansion?.packs.find(p => p.id === override.targetPackId)
  if (pack) {
    pack.image = `https://raw.githubusercontent.com/PocketDecks/pokemon-tcg-pocket-cards/refs/heads/main/images/webp/packs/${override.imageId}.webp`
  }
}

// The gameplay payload has one record per distinct card (2,822), while the collection
// lists every print (3,879) — alt-art and full-art prints have no gameplay record of
// their own. Each base card lists its other prints in `alternate_versions`, so index
// those to give every print the gameplay data of the card it reprints.
const gameplayById = new Map<string, GameplayEntry>(
  (gameplayRecords as GameplayEntry[]).map(entry => [entry.id, entry]),
)

const gameplayIdByPrintId = new Map<string, string>()
for (const card of cardsById.values()) {
  if (!gameplayById.has(card.id)) continue
  for (const print of card.alternate_versions ?? []) {
    gameplayIdByPrintId.set(printCardId(print.set_code, print.id), card.id)
  }
}

function gameplayFor(cardId: string): GameplayEntry | undefined {
  return gameplayById.get(cardId) ?? gameplayById.get(gameplayIdByPrintId.get(cardId) ?? '')
}

const metaById = new Map<string, CardMeta>()
for (const card of cardsById.values()) {
  const gameplay = gameplayFor(card.id)
  if (gameplay) metaById.set(card.id, deriveCardMeta(gameplay))
}

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
    getMeta: (cardId: string): CardMeta | undefined => metaById.get(cardId),
    getGameplay: (cardId: string): GameplayEntry | undefined => gameplayFor(cardId),
    getCardsBySet: (setCode: string): CardCatalogEntry[] => cardsBySetCode.get(setCode) ?? [],
    getAllCards: (): CardCatalogEntry[] => allCardsSorted,
    getExpansions: (): ExpansionEntry[] => expansionsSortedByRelease,
    getExpansion: (setCode: string): ExpansionEntry | undefined => expansionsByCode.get(setCode),
  }
}
