<script setup lang="ts">
import { cn } from '@/lib/utils'

const linkClass = ({ isActive }: { isActive: boolean }) =>
  cn('text-sm font-medium', isActive ? 'text-foreground' : 'text-muted-foreground hover:text-foreground')
</script>

<template>
  <nav class="flex min-w-0 items-center gap-2 overflow-x-auto sm:gap-4">
    <RouterLink v-slot="{ isActive, href, navigate }" to="/" custom>
      <a :href="href" :class="cn(linkClass({ isActive }), 'whitespace-nowrap')" @click="navigate">Collection</a>
    </RouterLink>
    <RouterLink v-slot="{ isActive, href, navigate }" to="/cards" custom>
      <a :href="href" :class="cn(linkClass({ isActive }), 'whitespace-nowrap')" @click="navigate">
        <span class="sm:hidden">Cards</span>
        <span class="hidden sm:inline">All cards</span>
      </a>
    </RouterLink>
    <RouterLink v-slot="{ isActive, href, navigate }" to="/trade-list" custom>
      <a :href="href" :class="cn(linkClass({ isActive }), 'whitespace-nowrap')" @click="navigate">
        <span class="sm:hidden">Trades</span>
        <span class="hidden sm:inline">Trade list</span>
      </a>
    </RouterLink>
  </nav>
</template>
