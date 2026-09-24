import { expect, test } from '@playwright/test'
import { registerViaApi } from './testUsers'

test('picking a set from the dropdown filters the grid and reveals that set\'s packs', async ({ page }) => {
  await registerViaApi(page)

  await page.goto('/cards')
  await expect(page.getByRole('heading', { name: 'All cards' })).toBeVisible()

  const setSelect = page.getByLabel('Filter by set')
  await expect(setSelect).toBeVisible()
  await expect(page.getByLabel('Filter by pack')).toHaveCount(0)

  const cardTiles = page.locator('.grid > div').filter({ has: page.locator('img') })
  await setSelect.selectOption({ label: 'Genetic Apex' })
  await expect(cardTiles).toHaveCount(286)

  // Choosing a set surfaces its own pack dropdown underneath.
  const packSelect = page.getByLabel('Filter by pack')
  await expect(packSelect).toBeVisible()
  const options = await packSelect.locator('option').allTextContents()
  expect(options).toEqual(expect.arrayContaining(['Charizard', 'Mewtwo', 'Pikachu']))

  // Switching sets updates both the set and pack filters.
  await setSelect.selectOption({ label: 'Mythical Island' })
  await expect(cardTiles).toHaveCount(86)

  // "Any set" clears the selection (and the now-irrelevant pack filter with it).
  await setSelect.selectOption({ label: 'Any set' })
  await expect(page.getByText('No cards match these filters.')).not.toBeVisible()
  await expect(page.getByLabel('Filter by pack')).toHaveCount(0)
})
