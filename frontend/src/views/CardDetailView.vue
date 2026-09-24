<script setup lang="ts">
import { ArrowLeft } from '@lucide/vue'
import { computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import CardArt from '@/components/cards/CardArt.vue'
import EnergyIcon from '@/components/cards/EnergyIcon.vue'
import EnergyText from '@/components/cards/EnergyText.vue'
import { useCardCatalog } from '@/composables/useCardCatalog'
import { useCollection } from '@/composables/useCollection'
import { useTradeList } from '@/composables/useTradeList'
import { type EnergyType, parseEnergyCost, printCardId } from '@/lib/cardMetadata'

const route = useRoute()
const router = useRouter()
const cardId = computed(() => route.params.cardId as string)

const { getCard, getGameplay, getMeta, getExpansion } = useCardCatalog()
const { getOwnedCount, ensureLoaded: ensureCollectionLoaded } = useCollection()
const { isWanted, isOffered, ensureLoaded: ensureTradeListLoaded } = useTradeList()

onMounted(() => Promise.all([ensureCollectionLoaded(), ensureTradeListLoaded()]))

const card = computed(() => getCard(cardId.value))
const gameplay = computed(() => getGameplay(cardId.value))
const meta = computed(() => getMeta(cardId.value))
const expansion = computed(() => (card.value ? getExpansion(card.value.set_code) : undefined))

const isPokemon = computed(() => gameplay.value?.type === 'Pokémon')
const attacks = computed(() =>
  [gameplay.value?.attacks?.['1'], gameplay.value?.attacks?.['2']].flatMap(attack => (attack?.name ? [attack] : [])))
const ability = computed(() => (gameplay.value?.ability?.exists ? gameplay.value.ability : null))
const weaknessType = computed(() => {
  const weakness = gameplay.value?.weakness
  return weakness && weakness !== 'none' ? weakness as EnergyType : null
})
const retreatCost = computed(() => Array.from({ length: gameplay.value?.retreat ?? 0 }, () => 'Colorless' as const))

const otherPrints = computed(() =>
  (card.value?.alternate_versions ?? []).flatMap((print) => {
    const id = printCardId(print.set_code, print.id)
    return getCard(id) ? [{ id, setName: print.set_name, rarity: print.rarity }] : []
  }))

const ownedCount = computed(() => getOwnedCount(cardId.value))

function goBack() {
  // Prefer real history so "back" returns to the filtered list the user came from.
  if (window.history.state?.back) router.back()
  else router.push('/cards')
}
</script>

<template>
  <div class="mx-auto max-w-5xl p-4 sm:p-6">
    <button type="button" class="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground" @click="goBack">
      <ArrowLeft class="size-4" /> Back
    </button>

    <p v-if="!card" class="mt-8 text-sm text-muted-foreground">
      Card not found. It may not exist in the current card data.
    </p>

    <div v-else class="mt-4 grid gap-6 md:grid-cols-[minmax(0,20rem)_1fr]">
      <CardArt :key="card.id" :card="card" class="md:sticky md:top-4 md:self-start" />

      <div class="flex min-w-0 flex-col gap-6">
        <header>
          <h1 class="text-2xl font-semibold">
            {{ card.name }}
          </h1>
          <p class="mt-1 text-sm text-muted-foreground">
            <template v-if="isPokemon">
              {{ gameplay?.stage }}<template v-if="gameplay?.evolves_from"> · Evolves from {{ gameplay.evolves_from }}</template>
            </template>
            <template v-else-if="gameplay">
              Trainer · {{ gameplay.subtype }}
            </template>
          </p>
          <p v-if="!gameplay" class="mt-2 text-sm text-muted-foreground">
            Battle details aren't available for this card.
          </p>
        </header>

        <dl v-if="isPokemon" class="grid grid-cols-2 gap-x-6 gap-y-4 sm:grid-cols-4">
          <div>
            <dt class="text-xs uppercase tracking-wide text-muted-foreground">
              HP
            </dt>
            <dd class="mt-1 text-lg font-semibold tabular-nums">
              {{ gameplay?.health }}
            </dd>
          </div>
          <div>
            <dt class="text-xs uppercase tracking-wide text-muted-foreground">
              Type
            </dt>
            <dd class="mt-1 flex items-center gap-1.5">
              <EnergyIcon v-if="meta?.pokemonType" :type="meta.pokemonType" />
              <template v-else>
                {{ gameplay?.subtype }}
              </template>
            </dd>
          </div>
          <div>
            <dt class="text-xs uppercase tracking-wide text-muted-foreground">
              Weakness
            </dt>
            <dd class="mt-1 flex items-center gap-1.5">
              <EnergyIcon v-if="weaknessType" :type="weaknessType" />
              <template v-else>
                None
              </template>
            </dd>
          </div>
          <div>
            <dt class="text-xs uppercase tracking-wide text-muted-foreground">
              Retreat cost
            </dt>
            <dd class="mt-1 flex items-center gap-1">
              <template v-if="retreatCost.length > 0">
                <EnergyIcon v-for="(type, index) in retreatCost" :key="index" :type="type" />
              </template>
              <template v-else>
                Free
              </template>
            </dd>
          </div>
        </dl>

        <section v-if="ability" aria-labelledby="ability-heading">
          <h2 id="ability-heading" class="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
            Ability
          </h2>
          <div class="mt-2 rounded-lg border p-3">
            <p class="font-medium">
              {{ ability.name }}
            </p>
            <p v-if="ability.effect" class="mt-1 text-sm text-muted-foreground">
              <EnergyText :text="ability.effect" />
            </p>
          </div>
        </section>

        <section v-if="attacks.length > 0" aria-labelledby="attacks-heading">
          <h2 id="attacks-heading" class="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
            Attacks
          </h2>
          <ul class="mt-2 flex flex-col gap-2">
            <li v-for="attack in attacks" :key="attack.name ?? ''" class="rounded-lg border p-3">
              <div class="flex items-center gap-3">
                <span class="flex min-w-14 shrink-0 items-center gap-1" :aria-label="`Cost: ${parseEnergyCost(attack.cost).length ? parseEnergyCost(attack.cost).join(', ') : 'no energy'}`">
                  <template v-if="parseEnergyCost(attack.cost).length > 0">
                    <EnergyIcon v-for="(type, index) in parseEnergyCost(attack.cost)" :key="index" :type="type" />
                  </template>
                  <span v-else class="text-xs text-muted-foreground">No energy</span>
                </span>
                <span class="min-w-0 flex-1 font-medium">{{ attack.name }}</span>
                <span v-if="attack.damage !== null" class="text-lg font-semibold tabular-nums">{{ attack.damage }}</span>
              </div>
              <p v-if="attack.effect" class="mt-2 text-sm text-muted-foreground">
                <EnergyText :text="attack.effect" />
              </p>
            </li>
          </ul>
        </section>

        <section v-if="gameplay && !isPokemon && gameplay.card_text" aria-labelledby="text-heading">
          <h2 id="text-heading" class="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
            Card text
          </h2>
          <p class="mt-2 rounded-lg border p-3 text-sm">
            <EnergyText :text="gameplay.card_text" />
          </p>
        </section>

        <p v-if="card.flavour_text" class="border-l-2 pl-3 text-sm italic text-muted-foreground">
          {{ card.flavour_text }}
        </p>

        <section aria-labelledby="details-heading">
          <h2 id="details-heading" class="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
            Details
          </h2>
          <dl class="mt-2 grid grid-cols-[auto_1fr] gap-x-6 gap-y-1.5 text-sm">
            <dt class="text-muted-foreground">
              Card
            </dt>
            <dd>{{ card.id }}</dd>
            <dt class="text-muted-foreground">
              Rarity
            </dt>
            <dd>{{ card.rarity }}</dd>
            <dt class="text-muted-foreground">
              Set
            </dt>
            <dd>
              <RouterLink :to="`/sets/${card.set_code}`" class="text-primary underline-offset-4 hover:underline">
                {{ expansion?.name ?? card.set_name }}
              </RouterLink>
            </dd>
            <template v-if="card.pack">
              <dt class="text-muted-foreground">
                Pack
              </dt>
              <dd>{{ card.pack }}</dd>
            </template>
            <template v-if="card.artist">
              <dt class="text-muted-foreground">
                Illustrator
              </dt>
              <dd>{{ card.artist }}</dd>
            </template>
            <template v-if="card.art_style">
              <dt class="text-muted-foreground">
                Art style
              </dt>
              <dd>{{ card.art_style }}</dd>
            </template>
            <template v-if="card.pack_points">
              <dt class="text-muted-foreground">
                Pack points
              </dt>
              <dd>{{ card.pack_points }}</dd>
            </template>
            <dt class="text-muted-foreground">
              Trading
            </dt>
            <dd>{{ card.tradable ? `Tradable (trade cost ${card.trade_cost ?? 0})` : 'Not tradable' }}</dd>
          </dl>
        </section>

        <section aria-labelledby="collection-heading">
          <h2 id="collection-heading" class="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
            In your collection
          </h2>
          <p class="mt-2 text-sm">
            <template v-if="ownedCount > 0">
              You own ×{{ ownedCount }}.
            </template>
            <template v-else>
              You don't own this card yet.
            </template>
            <template v-if="isWanted(card.id)">
              It's on your want list.
            </template>
            <template v-if="isOffered(card.id)">
              It's on your offer list.
            </template>
          </p>
        </section>

        <section v-if="otherPrints.length > 0" aria-labelledby="prints-heading">
          <h2 id="prints-heading" class="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
            Other prints
          </h2>
          <ul class="mt-2 flex flex-wrap gap-2">
            <li v-for="print in otherPrints" :key="print.id">
              <RouterLink :to="`/cards/${print.id}`" class="inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-sm hover:bg-accent">
                {{ print.id }} <span class="text-muted-foreground">· {{ print.rarity }} · {{ print.setName }}</span>
              </RouterLink>
            </li>
          </ul>
        </section>
      </div>
    </div>
  </div>
</template>
