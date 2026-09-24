import type { GameplayEntry } from '@/types/catalog'

export type EnergyType = 'Grass' | 'Fire' | 'Water' | 'Lightning' | 'Psychic' | 'Fighting' | 'Darkness' | 'Metal' | 'Dragon' | 'Colorless'
export type CardTypeFilter = 'pokemon' | 'trainer' | 'item' | 'supporter' | 'tool' | 'stadium'
export type EvolutionFilter = 'Basic' | 'Stage 1' | 'Stage 2' | 'ex' | 'Mega'
export type AbilityFilter = 'yes' | 'no'
type TrainerKind = Exclude<CardTypeFilter, 'pokemon' | 'trainer'>
type PokemonStage = Extract<EvolutionFilter, 'Basic' | 'Stage 1' | 'Stage 2'>

export interface FilterOption<T extends string> {
  value: T
  label: string
}

/** Everything the filters and search need to know about a card, derived once from its gameplay record. */
export interface CardMeta {
  isPokemon: boolean
  trainerKind: TrainerKind | undefined
  /** The Pokémon's own energy type; undefined for trainers. */
  pokemonType: EnergyType | undefined
  stage: PokemonStage | undefined
  ex: boolean
  mega: boolean
  hasAbility: boolean
  /** Distinct energy types that appear in this card's attack costs. */
  moveTypes: EnergyType[]
  /** Lowercased attack/ability names and effect text, for search. */
  searchText: string
}

export const ENERGY_TYPES: EnergyType[] = ['Grass', 'Fire', 'Water', 'Lightning', 'Psychic', 'Fighting', 'Darkness', 'Metal', 'Dragon', 'Colorless']

// Attack costs are written as strings of energy letters, e.g. "GGC" = two Grass + one Colorless.
const ENERGY_BY_COST_LETTER: Record<string, EnergyType> = {
  G: 'Grass',
  R: 'Fire',
  W: 'Water',
  L: 'Lightning',
  P: 'Psychic',
  F: 'Fighting',
  D: 'Darkness',
  M: 'Metal',
  C: 'Colorless',
}

export const CARD_TYPE_OPTIONS: FilterOption<CardTypeFilter>[] = [
  { value: 'pokemon', label: 'Pokémon' },
  { value: 'trainer', label: 'Trainer (all)' },
  { value: 'item', label: 'Item' },
  { value: 'supporter', label: 'Supporter' },
  { value: 'tool', label: 'Pokémon Tool' },
  { value: 'stadium', label: 'Stadium' },
]

export const EVOLUTION_OPTIONS: FilterOption<EvolutionFilter>[] = [
  { value: 'Basic', label: 'Basic' },
  { value: 'Stage 1', label: 'Stage 1' },
  { value: 'Stage 2', label: 'Stage 2' },
  { value: 'ex', label: 'ex' },
  { value: 'Mega', label: 'Mega' },
]

export const ABILITY_OPTIONS: FilterOption<AbilityFilter>[] = [
  { value: 'yes', label: 'Has an ability' },
  { value: 'no', label: 'No ability' },
]

const TRAINER_KINDS: Record<string, TrainerKind> = {
  Item: 'item',
  Supporter: 'supporter',
  'Pokémon Tool': 'tool',
  Tool: 'tool',
  Stadium: 'stadium',
}

const POKEMON_STAGES: readonly string[] = ['Basic', 'Stage 1', 'Stage 2']

function isEnergyType(value: string): value is EnergyType {
  return (ENERGY_TYPES as string[]).includes(value)
}

export function deriveCardMeta(entry: GameplayEntry): CardMeta {
  const isPokemon = entry.type === 'Pokémon'
  const attacks = [entry.attacks?.['1'], entry.attacks?.['2']].filter(attack => attack !== undefined && attack.name)

  const moveTypes = new Set<EnergyType>()
  for (const attack of attacks) {
    for (const letter of attack?.cost ?? '') {
      const energy = ENERGY_BY_COST_LETTER[letter]
      if (energy) moveTypes.add(energy)
    }
  }

  const ability = entry.ability
  const searchText = [
    ...attacks.flatMap(attack => [attack?.name, attack?.effect]),
    ability?.exists ? ability.name : null,
    ability?.exists ? ability.effect : null,
    entry.card_text,
  ].filter((text): text is string => !!text).join('\n').toLowerCase()

  return {
    isPokemon,
    trainerKind: isPokemon ? undefined : TRAINER_KINDS[entry.subtype],
    pokemonType: isPokemon && isEnergyType(entry.subtype) ? entry.subtype : undefined,
    // Trainers (e.g. fossils) can carry a "Basic" stage too, but evolution is a Pokémon-only idea.
    stage: isPokemon && entry.stage && POKEMON_STAGES.includes(entry.stage) ? entry.stage as PokemonStage : undefined,
    ex: entry.ex === true,
    mega: entry.mega === true,
    hasAbility: ability?.exists === true,
    moveTypes: [...moveTypes],
    searchText,
  }
}

export function matchesCardType(meta: CardMeta, filter: CardTypeFilter): boolean {
  switch (filter) {
    case 'pokemon': return meta.isPokemon
    case 'trainer': return !meta.isPokemon
    default: return meta.trainerKind === filter
  }
}

export function matchesEvolution(meta: CardMeta, filter: EvolutionFilter): boolean {
  if (filter === 'ex') return meta.ex
  if (filter === 'Mega') return meta.mega
  return meta.stage === filter
}

/** Ability filters only make sense for Pokémon; trainers are excluded from both "has" and "no" ability. */
export function matchesAbility(meta: CardMeta, filter: AbilityFilter): boolean {
  return meta.isPokemon && meta.hasAbility === (filter === 'yes')
}
