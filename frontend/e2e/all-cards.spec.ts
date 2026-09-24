import { expect, test } from '@playwright/test'
import { registerViaApi } from './testUsers'

test('searching and filtering the all-cards page spans every set', async ({ page }) => {
  await registerViaApi(page)

  await page.goto('/cards')
  await expect(page.getByRole('heading', { name: 'All cards' })).toBeVisible()
  await expect(page.getByText('3879 / 3879 cards')).toBeVisible()

  // Nothing renders until a filter narrows things down — rendering all ~3,879
  // cards by default is a real perf hit, not just a display choice.
  await expect(page.getByText(/cards across every set/)).toBeVisible()
  await expect(page.locator('.grid > div')).toHaveCount(0)

  await page.getByPlaceholder('Search by name or number').fill('pikachu')
  const matchedCards = page.locator('.grid > div').filter({ has: page.locator('img') })
  await expect(matchedCards.first()).toBeVisible()
  const matchedCount = await matchedCards.count()
  expect(matchedCount).toBeGreaterThan(1)
  for (const name of await matchedCards.locator('p.font-medium').allInnerTexts()) {
    expect(name.toLowerCase()).toContain('pikachu')
  }

  // Pikachu cards span multiple sets, proving the search isn't scoped to one.
  const cardIds = await matchedCards.locator('p.text-xs').allInnerTexts()
  const setCodes = new Set(cardIds.map(text => text.split('-')[0].trim()))
  expect(setCodes.size).toBeGreaterThan(1)

  await page.getByPlaceholder('Search by name or number').fill('')
  await page.getByLabel('Filter by set').selectOption({ label: 'Mythical Island' })
  await expect(matchedCards).toHaveCount(86)
  for (const name of await matchedCards.locator('p.text-xs').allInnerTexts()) {
    expect(name.startsWith('a1a-')).toBe(true)
  }

  await page.getByRole('button', { name: 'Clear' }).click()
  await expect(page.getByText('3879 / 3879 cards')).toBeVisible()
  await expect(page.getByText(/cards across every set/)).toBeVisible()
})

test('all cards page is reachable from the nav and shares ownership state with a set page', async ({ page }) => {
  await registerViaApi(page)

  await page.goto('/sets/a1')
  await expect(page.getByRole('heading', { name: 'Genetic Apex' })).toBeVisible()
  const firstCard = page.locator('.grid > div').filter({ has: page.locator('img') }).first()
  await firstCard.getByRole('button').nth(1).click()
  await expect(firstCard.getByText('1', { exact: true })).toBeVisible()

  await page.getByRole('link', { name: 'All cards' }).click()
  await expect(page.getByRole('heading', { name: 'All cards' })).toBeVisible()

  await page.getByPlaceholder('Search by name or number').fill('bulbasaur')
  const bulbasaurCard = page.locator('.grid > div').filter({ has: page.locator('img') }).first()
  await expect(bulbasaurCard.getByText('1', { exact: true })).toBeVisible()
})
