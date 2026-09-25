<script setup lang="ts">
import { cn } from '@/lib/utils'
import { navLinks } from '@/components/layout/navLinks'

// Horizontal by default (the inline header menu); vertical is the list inside the hamburger dropdown.
defineProps<{ vertical?: boolean }>()

const linkClass = ({ isActive }: { isActive: boolean }) =>
  isActive ? 'text-foreground' : 'text-muted-foreground hover:text-foreground'
</script>

<template>
  <nav :class="vertical ? 'flex flex-col py-1' : 'flex min-w-0 items-center gap-2 overflow-x-auto sm:gap-4'">
    <RouterLink v-for="link in navLinks" :key="link.to" v-slot="{ isActive, href, navigate }" :to="link.to" custom>
      <a
        :href="href"
        :class="cn(
          'whitespace-nowrap font-medium',
          vertical ? 'px-4 py-3 text-base hover:bg-accent' : 'text-sm',
          linkClass({ isActive }),
        )"
        :aria-current="isActive ? 'page' : undefined"
        @click="navigate"
      >
        <template v-if="vertical || !link.shortLabel">{{ link.label }}</template>
        <template v-else>
          <span class="sm:hidden">{{ link.shortLabel }}</span>
          <span class="hidden sm:inline">{{ link.label }}</span>
        </template>
      </a>
    </RouterLink>
  </nav>
</template>
