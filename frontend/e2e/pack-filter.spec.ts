import { expect, test } from '@playwright/test'
import { registerViaApi } from './testUsers'

test('picking a pack from the dropdown filters the grid to that pack, and clearing it removes the filter', async ({ page }) => {
  await registerViaApi(page)

  await page.goto('/sets/a1')
  await expect(page.getByRole('heading', { name: 'Genetic Apex' })).toBeVisible()

  const packSelect = page.getByLabel('Filter by pack')
  const options = (await packSelect.locator('option').allTextContents()).map(text => text.trim())
  expect(options).toEqual(expect.arrayContaining(['Any pack', 'Charizard', 'Mewtwo', 'Pikachu']))

  const cardTiles = page.locator('.grid > div').filter({ has: page.locator('img') })
  await expect(cardTiles).toHaveCount(286)

  await packSelect.selectOption('Charizard')
  await expect(cardTiles).toHaveCount(80)
  for (const name of await cardTiles.locator('p.text-xs').allInnerTexts()) {
    expect(name.startsWith('a1-')).toBe(true)
  }

  // Picking "Any pack" again clears it back to every card in the set.
  await packSelect.selectOption('')
  await expect(cardTiles).toHaveCount(286)

  await packSelect.selectOption('Pikachu')
  await expect(cardTiles).not.toHaveCount(286)
  await page.getByRole('button', { name: 'Clear' }).click()
  await expect(cardTiles).toHaveCount(286)
})

test('the pack dropdown is hidden until a set with more than one pack is in view', async ({ page }) => {
  await registerViaApi(page)

  // a1a (Mythical Island) has exactly one pack — nothing meaningful to filter by.
  await page.goto('/sets/a1a')
  await expect(page.getByRole('heading', { name: 'Mythical Island' })).toBeVisible()
  await expect(page.getByLabel('Filter by pack')).toHaveCount(0)

  // On the all-cards page, the dropdown only appears once a specific set is chosen.
  await page.goto('/cards')
  await expect(page.getByRole('heading', { name: 'All cards' })).toBeVisible()
  await expect(page.getByLabel('Filter by pack')).toHaveCount(0)

  await page.getByLabel('Filter by set').selectOption({ label: 'Genetic Apex' })
  const packSelect = page.getByLabel('Filter by pack')
  await expect(packSelect).toBeVisible()
  const options = await packSelect.locator('option').allTextContents()
  expect(options).toEqual(expect.arrayContaining(['Charizard', 'Mewtwo', 'Pikachu']))
})
