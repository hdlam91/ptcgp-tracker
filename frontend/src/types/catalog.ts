import type { PokemonTCGPocketCardsV5CollectionSchema } from 'pokemon-tcg-pocket-cards/v5/collection'
import type { PokemonTCGPocketExpansionsV5Schema } from 'pokemon-tcg-pocket-cards/v5/expansions'
import type { PokemonTCGPocketCardsV5GameplayNoImageSchema } from 'pokemon-tcg-pocket-cards/v5/gameplay/no-image'

/** One card record from the pokemon-tcg-pocket-cards "collection" payload. */
export type CardCatalogEntry = PokemonTCGPocketCardsV5CollectionSchema[number]

/** One expansion/set record from the pokemon-tcg-pocket-cards "expansions" payload. */
export type ExpansionEntry = PokemonTCGPocketExpansionsV5Schema[number]

/** One record from the "gameplay" payload: HP, energy type, attacks, ability, trainer text. */
export type GameplayEntry = PokemonTCGPocketCardsV5GameplayNoImageSchema[number]
