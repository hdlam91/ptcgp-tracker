<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  owned: number
  total: number
}>()

const percent = computed(() => (props.total === 0 ? 0 : Math.round((props.owned / props.total) * 100)))
</script>

<template>
  <div class="flex flex-col gap-1">
    <div class="flex items-center justify-between text-xs text-muted-foreground">
      <span>{{ owned }} / {{ total }}</span>
      <span>{{ percent }}%</span>
    </div>
    <div class="h-1.5 w-full overflow-hidden rounded-full bg-muted">
      <div class="h-full rounded-full bg-primary transition-all" :style="{ width: `${percent}%` }" />
    </div>
  </div>
</template>
