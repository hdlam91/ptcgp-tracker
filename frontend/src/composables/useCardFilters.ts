import type { ComputedRef, Ref } from 'vue'
import { computed, ref, watch } from 'vue'
import { useCardCatalog } from '@/composables/useCardCatalog'
import {
  ABILITY_OPTIONS,
  type AbilityFilter,
  CARD_TYPE_OPTIONS,
  type CardTypeFilter,
  ENERGY_TYPES,
  type EnergyType,
  EVOLUTION_OPTIONS,
  type EvolutionFilter,
  type FilterOption,
  matchesAbility,
  matchesCardType,
  matchesEvolution,
} from '@/lib/cardMetadata'
import type { CardCatalogEntry, ExpansionEntry } from '@/types/catalog'

export type OwnershipFilter = 'all' | 'owned' | 'missing'
export type PackOption = ExpansionEntry['packs'][number]

type Rarity = CardCatalogEntry['rarity']

// Dataset rarity values in ascending order, so the dropdown reads low-to-high
// instead of following whatever order cards happen to appear in a set.
const RARITY_ORDER = ['◊', '◊◊', '◊◊◊', '◊◊◊◊', '☆', '☆☆', '☆☆☆', 'Crown Rare', 'Promo']

/**
 * Search (name, number, attack/ability names and effect text) plus set, pack, rarity,
 * card type, Pokémon type, evolution stage, ability, move type, and ownership filtering over a
 * list of catalog cards. Works the same whether `cards` is scoped to one set (the
 * set filter then has nothing to show, per `setOptions.length <= 1`) or spans the
 * whole catalog. `getOwnedCount` is only consulted when an ownership filter is
 * active, so it can be omitted on read-only views that have no ownership state.
 */
export function useCardFilters(
  cards: Ref<CardCatalogEntry[]> | ComputedRef<CardCatalogEntry[]>,
  getOwnedCount?: (cardId: string) => number,
) {
  const { getExpansions, getExpansion, getMeta } = useCardCatalog()

  const search = ref('')
  const setCode = ref('')
  const rarity = ref<Rarity | ''>('')
  const pack = ref('')
  const ownership = ref<OwnershipFilter>('all')
  const cardType = ref<CardTypeFilter | ''>('')
  const pokemonType = ref<EnergyType | ''>('')
  const evolution = ref<EvolutionFilter | ''>('')
  const ability = ref<AbilityFilter | ''>('')
  const moveType = ref<EnergyType | ''>('')

  const setOptions = computed(() => {
    const present = new Set(cards.value.map(card => card.set_code))
    return getExpansions().filter(expansion => present.has(expansion.id))
  })

  const rarityOptions = computed(() => {
    const present = new Set(cards.value.map(card => card.rarity))
    return RARITY_ORDER.filter((r): r is Rarity => present.has(r as Rarity))
  })

  // The pack dropdown is only meaningful once exactly one set is in view —
  // either because the set filter picked one, or because the incoming card
  // list already happens to be scoped to a single set (e.g. a set's own page).
  const packSetCode = computed(() => {
    if (setCode.value) return setCode.value
    const codes = new Set(cards.value.map(card => card.set_code))
    return codes.size === 1 ? [...codes][0] : undefined
  })

  const packOptions = computed<PackOption[]>(() => {
    if (!packSetCode.value) return []
    const expansion = getExpansion(packSetCode.value)
    if (!expansion) return []

    const present = new Set(
      cards.value
        .filter(card => card.set_code === packSetCode.value)
        .map(card => card.pack)
        .filter((p): p is string => !!p),
    )
    return expansion.packs.filter(pack => present.has(pack.name))
  })

  // Like the rarity dropdown, each metadata dropdown only offers values that exist
  // in the cards in view, so a set with no Stadiums doesn't list "Stadium".
  const advancedOptions = computed(() => {
    const metas = cards.value.flatMap((card) => {
      const meta = getMeta(card.id)
      return meta ? [meta] : []
    })
    const energyOptions = (present: Set<EnergyType>): FilterOption<EnergyType>[] =>
      ENERGY_TYPES.filter(type => present.has(type)).map(type => ({ value: type, label: type }))

    return {
      cardTypes: CARD_TYPE_OPTIONS.filter(option => metas.some(meta => matchesCardType(meta, option.value))),
      pokemonTypes: energyOptions(new Set(metas.flatMap(meta => (meta.pokemonType ? [meta.pokemonType] : [])))),
      evolutions: EVOLUTION_OPTIONS.filter(option => metas.some(meta => matchesEvolution(meta, option.value))),
      abilities: metas.some(meta => meta.isPokemon) ? ABILITY_OPTIONS : [],
      moveTypes: energyOptions(new Set(metas.flatMap(meta => meta.moveTypes))),
    }
  })

  const filteredCards = computed(() => {
    const term = search.value.trim().toLowerCase()
    return cards.value.filter((card) => {
      const meta = getMeta(card.id)
      if (
        term
        && !card.name.toLowerCase().includes(term)
        && !card.id.toLowerCase().includes(term)
        && !meta?.searchText.includes(term)
      ) {
        return false
      }
      if (setCode.value && card.set_code !== setCode.value) {
        return false
      }
      if (rarity.value && card.rarity !== rarity.value) {
        return false
      }
      if (pack.value && card.pack !== pack.value) {
        return false
      }
      if (cardType.value && !(meta && matchesCardType(meta, cardType.value))) return false
      if (pokemonType.value && meta?.pokemonType !== pokemonType.value) return false
      if (evolution.value && !(meta && matchesEvolution(meta, evolution.value))) return false
      if (ability.value && !(meta && matchesAbility(meta, ability.value))) return false
      if (moveType.value && !meta?.moveTypes.includes(moveType.value)) return false
      if (ownership.value !== 'all' && getOwnedCount) {
        const owned = getOwnedCount(card.id) > 0
        if (ownership.value === 'owned' && !owned) return false
        if (ownership.value === 'missing' && owned) return false
      }
      return true
    })
  })

  const hasActiveFilters = computed(() =>
    search.value !== '' || setCode.value !== '' || rarity.value !== '' || pack.value !== '' || ownership.value !== 'all'
    || cardType.value !== '' || pokemonType.value !== '' || evolution.value !== '' || ability.value !== '' || moveType.value !== '')

  function resetFilters() {
    search.value = ''
    setCode.value = ''
    rarity.value = ''
    pack.value = ''
    ownership.value = 'all'
    cardType.value = ''
    pokemonType.value = ''
    evolution.value = ''
    ability.value = ''
    moveType.value = ''
  }

  // Changing the set invalidates whatever pack was selected for the old set.
  watch(setCode, () => { pack.value = '' })

  return {
    search,
    setCode,
    rarity,
    pack,
    ownership,
    cardType,
    pokemonType,
    evolution,
    ability,
    moveType,
    advancedOptions,
    setOptions,
    rarityOptions,
    packOptions,
    filteredCards,
    hasActiveFilters,
    resetFilters,
  }
}
