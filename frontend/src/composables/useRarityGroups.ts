import type { ComputedRef, Ref } from 'vue'
import { computed } from 'vue'
import type { CardCatalogEntry } from '@/types/catalog'

export interface RarityGroup {
  key: string
  label: string
  owned: number
  total: number
  missingCardIds: string[]
}

// Grouped rather than shown per-tier (◊ vs ◊◊◊◊ etc.) since "diamond/star/crown
// completion" is how players talk about a set's rarity ladder, not the individual tiers.
const RARITY_GROUPS: { key: string, label: string, rarities: CardCatalogEntry['rarity'][] }[] = [
  { key: 'diamond', label: 'Diamond', rarities: ['◊', '◊◊', '◊◊◊', '◊◊◊◊'] },
  { key: 'star', label: 'Star', rarities: ['☆', '☆☆', '☆☆☆'] },
  { key: 'crown', label: 'Crown', rarities: ['Crown Rare'] },
]

/**
 * Owned/total + which card IDs are still missing, per rarity group, for a set's cards.
 * A plain function (not itself a computed) so it can be called once per set when
 * rendering a list of sets, not just once for a single set's own page.
 */
export function computeRarityGroups(
  cards: CardCatalogEntry[],
  getOwnedCount: (cardId: string) => number,
): RarityGroup[] {
  return RARITY_GROUPS
    .map(({ key, label, rarities }) => {
      const cardsInGroup = cards.filter(card => rarities.includes(card.rarity))
      const missingCardIds = cardsInGroup.filter(card => getOwnedCount(card.id) === 0).map(card => card.id)
      return {
        key,
        label,
        total: cardsInGroup.length,
        owned: cardsInGroup.length - missingCardIds.length,
        missingCardIds,
      }
    })
    // A set without any cards of a given rarity (e.g. no Crown Rares) shouldn't show an empty 0/0 row.
    .filter(group => group.total > 0)
}

/** Reactive wrapper around {@link computeRarityGroups} for a single set's own page. */
export function useRarityGroups(
  cards: Ref<CardCatalogEntry[]> | ComputedRef<CardCatalogEntry[]>,
  getOwnedCount: (cardId: string) => number,
) {
  const groups = computed<RarityGroup[]>(() => computeRarityGroups(cards.value, getOwnedCount))
  return { groups }
}
