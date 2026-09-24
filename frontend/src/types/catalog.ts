import type { PokemonTCGPocketCardsV5CollectionSchema } from 'pokemon-tcg-pocket-cards/v5/collection'
import type { PokemonTCGPocketExpansionsV5Schema } from 'pokemon-tcg-pocket-cards/v5/expansions'

/** One card record from the pokemon-tcg-pocket-cards "collection" payload. */
export type CardCatalogEntry = PokemonTCGPocketCardsV5CollectionSchema[number]

/** One expansion/set record from the pokemon-tcg-pocket-cards "expansions" payload. */
export type ExpansionEntry = PokemonTCGPocketExpansionsV5Schema[number]
