<script setup lang="ts">
import { computed } from 'vue'
import EnergyIcon from '@/components/cards/EnergyIcon.vue'
import { splitEnergyText } from '@/lib/cardMetadata'

const props = defineProps<{ text: string }>()

const parts = computed(() => splitEnergyText(props.text))
</script>

<template>
  <span>
    <template v-for="(part, index) in parts" :key="index">
      <EnergyIcon v-if="'energy' in part" :type="part.energy" size="sm" class="mx-0.5 align-text-bottom" />
      <template v-else>{{ part.text }}</template>
    </template>
  </span>
</template>
