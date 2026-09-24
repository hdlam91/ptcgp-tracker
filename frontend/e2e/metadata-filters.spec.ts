import { expect, test } from '@playwright/test'
import { registerViaApi } from './testUsers'

// Expected counts come from the pinned pokemon-tcg-pocket-cards v5.3.1 dataset, tallied
// separately from the app's own code (Genetic Apex = 286 cards, including alt-art prints).
const A1_TOTAL = 286

async function openSet(page: import('@playwright/test').Page) {
  await registerViaApi(page)
  await page.goto('/sets/a1')
  await expect(page.getByRole('heading', { name: 'Genetic Apex' })).toBeVisible()
  await expect(page.getByText(`${A1_TOTAL} / ${A1_TOTAL} cards`)).toBeVisible()
  await page.getByRole('button', { name: 'Filters' }).click()
}

test('card type filter separates Pokémon from each kind of trainer', async ({ page }) => {
  await openSet(page)
  const cardType = page.getByLabel('Filter by card type')

  await cardType.selectOption({ label: 'Pokémon' })
  await expect(page.getByText(`267 / ${A1_TOTAL} cards`)).toBeVisible()

  await cardType.selectOption({ label: 'Trainer (all)' })
  await expect(page.getByText(`19 / ${A1_TOTAL} cards`)).toBeVisible()

  await cardType.selectOption({ label: 'Supporter' })
  await expect(page.getByText(`16 / ${A1_TOTAL} cards`)).toBeVisible()

  await cardType.selectOption({ label: 'Item' })
  await expect(page.getByText(`3 / ${A1_TOTAL} cards`)).toBeVisible()
})

test('Pokémon type, evolution, ability and move type filters narrow the set', async ({ page }) => {
  await openSet(page)

  await page.getByLabel('Filter by Pokémon type').selectOption({ label: 'Fire' })
  await expect(page.getByText(`28 / ${A1_TOTAL} cards`)).toBeVisible()
  await page.getByLabel('Filter by Pokémon type').selectOption({ label: 'Any' })

  await page.getByLabel('Filter by evolution').selectOption({ label: 'Stage 2' })
  await expect(page.getByText(`40 / ${A1_TOTAL} cards`)).toBeVisible()
  await page.getByLabel('Filter by evolution').selectOption({ label: 'ex' })
  await expect(page.getByText(`42 / ${A1_TOTAL} cards`)).toBeVisible()
  await page.getByLabel('Filter by evolution').selectOption({ label: 'Any' })

  await page.getByLabel('Filter by ability').selectOption({ label: 'Has an ability' })
  await expect(page.getByText(`18 / ${A1_TOTAL} cards`)).toBeVisible()
  await page.getByLabel('Filter by ability').selectOption({ label: 'No ability' })
  // Pokémon only: trainers have no ability, but "no ability" isn't meant to list them.
  await expect(page.getByText(`249 / ${A1_TOTAL} cards`)).toBeVisible()
  await page.getByLabel('Filter by ability').selectOption({ label: 'Any' })

  await page.getByLabel('Filter by move type').selectOption({ label: 'Water' })
  await expect(page.getByText(`41 / ${A1_TOTAL} cards`)).toBeVisible()
})

test('filters combine, show how many are active, and clear together', async ({ page }) => {
  await openSet(page)

  await page.getByLabel('Filter by Pokémon type').selectOption({ label: 'Water' })
  await page.getByLabel('Filter by evolution').selectOption({ label: 'Stage 1' })
  await expect(page.getByText(`19 / ${A1_TOTAL} cards`)).toBeVisible()
  await expect(page.getByRole('button', { name: /Filters\s*2/ })).toBeVisible()

  await page.getByRole('button', { name: 'Clear' }).click()
  await expect(page.getByText(`${A1_TOTAL} / ${A1_TOTAL} cards`)).toBeVisible()
  await expect(page.getByLabel('Filter by Pokémon type')).toHaveValue('')
  await expect(page.getByLabel('Filter by evolution')).toHaveValue('')
})

test('search matches attack names (including alt-art prints) and ability names', async ({ page }) => {
  await registerViaApi(page)
  await page.goto('/sets/a1')
  const search = page.getByPlaceholder('Search name, number, attack, ability')

  // Vine Whip is Bulbasaur's attack; four Genetic Apex cards carry it, and the ☆ alt-art
  // Bulbasaur (a1-227) has no gameplay record of its own — it inherits the base card's.
  await search.fill('vine whip')
  await expect(page.getByText(`4 / ${A1_TOTAL} cards`)).toBeVisible()
  await expect(page.getByText('a1-001', { exact: false }).first()).toBeVisible()
  await expect(page.getByText('a1-227', { exact: false }).first()).toBeVisible()

  await search.fill('powder heal')
  await expect(page.getByText(`1 / ${A1_TOTAL} cards`)).toBeVisible()
  await expect(page.getByText('Butterfree').first()).toBeVisible()
})

test('all-cards page can filter the whole catalog by evolution', async ({ page }) => {
  await registerViaApi(page)
  await page.goto('/cards')
  await expect(page.getByText(/cards across every set/)).toBeVisible()

  await page.getByRole('button', { name: 'Filters' }).click()
  await page.getByLabel('Filter by evolution').selectOption({ label: 'Mega' })
  await expect(page.getByText('111 / 3879 cards')).toBeVisible()
})
