<script setup lang="ts">
import { cn } from '@/lib/utils'

const linkClass = ({ isActive }: { isActive: boolean }) =>
  cn('text-sm font-medium', isActive ? 'text-foreground' : 'text-muted-foreground hover:text-foreground')
</script>

<template>
  <nav class="flex items-center gap-3 sm:gap-4">
    <RouterLink v-slot="{ isActive, href, navigate }" to="/" custom>
      <a :href="href" :class="cn(linkClass({ isActive }), 'whitespace-nowrap')" @click="navigate">Collection</a>
    </RouterLink>
    <RouterLink v-slot="{ isActive, href, navigate }" to="/trade-list" custom>
      <a :href="href" :class="cn(linkClass({ isActive }), 'whitespace-nowrap')" @click="navigate">Trade list</a>
    </RouterLink>
  </nav>
</template>
