<script setup lang="ts">
import type { RarityGroup } from '@/composables/useRarityGroups'
import { Check } from '@lucide/vue'
import { useI18n } from 'vue-i18n'
import { Button } from '@/components/ui/button'

defineProps<{
  groups: RarityGroup[]
  /** Group key currently being bulk-completed, if any — disables its button and shows progress. */
  completingKey: string | null
}>()

defineEmits<{
  (e: 'complete', key: string): void
}>()

const { t } = useI18n()
</script>

<template>
  <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
    <div v-for="group in groups" :key="group.key" class="flex flex-col gap-2 rounded-lg border bg-card p-3">
      <div class="flex items-center justify-between gap-2">
        <span class="text-sm font-medium">{{ group.label }}</span>
        <span v-if="group.owned === group.total" class="flex items-center gap-1 text-xs font-medium text-primary">
          <Check class="size-3.5" /> {{ t('cards.rarityProgressPanel.complete') }}
        </span>
        <span v-else class="text-xs text-muted-foreground">{{ group.owned }} / {{ group.total }}</span>
      </div>
      <div class="h-1.5 w-full overflow-hidden rounded-full bg-muted">
        <div
          class="h-full rounded-full bg-primary transition-all"
          :style="{ width: `${group.total === 0 ? 0 : Math.round((group.owned / group.total) * 100)}%` }"
        />
      </div>
      <Button
        v-if="group.owned < group.total"
        variant="outline"
        size="sm"
        class="mt-1 self-start"
        :disabled="completingKey === group.key"
        @click="$emit('complete', group.key)"
      >
        {{ completingKey === group.key ? t('cards.rarityProgressPanel.completing') : t('cards.rarityProgressPanel.markAllOwned', { label: group.label.toLowerCase() }) }}
      </Button>
    </div>
  </div>
</template>
