<script setup lang="ts">
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
</script>

<template>
  <div class="mx-auto flex max-w-2xl flex-col gap-6 px-4 py-10">
    <div>
      <RouterLink to="/" class="text-sm text-muted-foreground hover:text-foreground">
        ← Back
      </RouterLink>
      <h1 class="mt-2 text-2xl font-semibold">
        Legal & data sources
      </h1>
    </div>

    <Card>
      <CardHeader>
        <CardTitle class="text-base">
          Not affiliated with Nintendo, Creatures, GAME FREAK, or The Pokémon Company
        </CardTitle>
      </CardHeader>
      <CardContent class="flex flex-col gap-3 text-sm text-muted-foreground">
        <p>
          PTCGP Tracker is an independent, fan-made, non-commercial collection-tracking tool
          for <em>Pokémon Trading Card Game Pocket</em>. It is not produced, hosted, endorsed,
          sponsored, or approved by Nintendo, Creatures Inc., GAME FREAK inc., or The Pokémon
          Company.
        </p>
        <p>
          Pokémon, Pokémon Trading Card Game Pocket, and all associated names, logos, and card
          designs are trademarks and copyrighted material of their respective owners. This site
          does not claim any ownership over that material.
        </p>
      </CardContent>
    </Card>

    <Card>
      <CardHeader>
        <CardTitle class="text-base">
          Card data & images
        </CardTitle>
      </CardHeader>
      <CardContent class="flex flex-col gap-3 text-sm text-muted-foreground">
        <p>
          Card names, rarities, set information, and card images shown on this site come from
          the community-maintained
          <a
            href="https://github.com/PocketDecks/pokemon-tcg-pocket-cards"
            target="_blank"
            rel="noopener noreferrer"
            class="font-medium text-primary underline-offset-4 hover:underline"
          >pokemon-tcg-pocket-cards</a>
          dataset, pinned to a fixed release. Card artwork ultimately originates from the
          official Pokémon Trading Card Game Pocket app and belongs to its respective rights
          holders; it's used here for identification purposes only, alongside the collection
          tracking this app actually provides.
        </p>
      </CardContent>
    </Card>

    <Card>
      <CardHeader>
        <CardTitle class="text-base">
          No warranty
        </CardTitle>
      </CardHeader>
      <CardContent class="text-sm text-muted-foreground">
        <p>
          This tool is provided "as is," with no guarantee that card data, rarities, or set
          completion figures are complete or error-free. If something looks wrong, it's a bug
          in this tracker or its data source, not an official record.
        </p>
      </CardContent>
    </Card>

    <p class="text-xs text-muted-foreground">
      Questions about a specific asset, or a rights holder who'd like something addressed? Open
      an issue on the project's repository.
    </p>
  </div>
</template>
