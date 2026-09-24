import { expect, test } from '@playwright/test'
import { registerViaApi } from './testUsers'

test('picking a set tile filters the grid and reveals that set\'s packs', async ({ page }) => {
  await registerViaApi(page)

  await page.goto('/cards')
  await expect(page.getByRole('heading', { name: 'All cards' })).toBeVisible()

  const setPicker = page.getByRole('group', { name: 'Filter by set' })
  await expect(setPicker.getByRole('button', { name: 'Genetic Apex' })).toBeVisible()
  await expect(page.getByLabel('Filter by pack')).toHaveCount(0)

  const cardTiles = page.locator('.grid > div').filter({ has: page.locator('img') })
  await setPicker.getByRole('button', { name: 'Genetic Apex' }).click()
  await expect(cardTiles).toHaveCount(286)

  // Choosing a set surfaces its own pack dropdown underneath.
  const packSelect = page.getByLabel('Filter by pack')
  await expect(packSelect).toBeVisible()
  const options = await packSelect.locator('option').allTextContents()
  expect(options).toEqual(expect.arrayContaining(['Charizard', 'Mewtwo', 'Pikachu']))

  // Toggling the same set off clears both the set and pack filters.
  await setPicker.getByRole('button', { name: 'Genetic Apex' }).click()
  await expect(page.getByText('No cards match these filters.')).not.toBeVisible()
  await expect(page.getByLabel('Filter by pack')).toHaveCount(0)

  // "All sets" also clears the selection.
  await setPicker.getByRole('button', { name: 'Mythical Island' }).click()
  await expect(cardTiles).toHaveCount(86)
  await setPicker.getByRole('button', { name: 'All sets' }).click()
  await expect(page.getByLabel('Filter by pack')).toHaveCount(0)
})
