<script setup lang="ts">
import CardGridItem from '@/components/cards/CardGridItem.vue'
import type { CardCatalogEntry } from '@/types/catalog'

defineProps<{
  cards: CardCatalogEntry[]
  ownedCounts: Map<string, number>
  wantedCardIds: Set<string>
  offeredCardIds: Set<string>
}>()

const emit = defineEmits<{
  (e: 'update:ownedCount', cardId: string, count: number): void
  (e: 'toggle-want', cardId: string): void
  (e: 'toggle-offer', cardId: string): void
}>()
</script>

<template>
  <div class="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6">
    <CardGridItem
      v-for="card in cards"
      :key="card.id"
      :card="card"
      :owned-count="ownedCounts.get(card.id) ?? 0"
      :wanted="wantedCardIds.has(card.id)"
      :offered="offeredCardIds.has(card.id)"
      @update:owned-count="(count) => emit('update:ownedCount', card.id, count)"
      @toggle-want="emit('toggle-want', card.id)"
      @toggle-offer="emit('toggle-offer', card.id)"
    />
  </div>
</template>
