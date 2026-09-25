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
// Shiny cards carry a ☆/☆☆ rarity in the dataset but are their own collecting goal, so they
// count under "shiny" only, not under "star".
const RARITY_GROUPS: { key: string, label: string, matches: (card: CardCatalogEntry) => boolean }[] = [
  { key: 'diamond', label: 'Diamond', matches: card => ['◊', '◊◊', '◊◊◊', '◊◊◊◊'].includes(card.rarity) },
  { key: 'star', label: 'Star', matches: card => ['☆', '☆☆', '☆☆☆'].includes(card.rarity) && !card.shiny },
  { key: 'shiny', label: 'Shiny', matches: card => card.shiny },
  { key: 'crown', label: 'Crown', matches: card => card.rarity === 'Crown Rare' },
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
    .map(({ key, label, matches }) => {
      const cardsInGroup = cards.filter(matches)
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
