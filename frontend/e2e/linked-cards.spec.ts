import { expect, test } from '@playwright/test'
import { registerViaApi } from './testUsers'

// b1a-001 and b4b-001 are the same real-world Bulbasaur card (the Deluxe Pack: Mega set reprints
// Crimson Blaze) per the backend's CardEquivalenceGrouping, so owning it through one set must show
// up on the other set's page too — without a reload, since the two pages are reached by client-side
// navigation and share the same in-memory ownedCounts.
test('owning a card through one set shows it owned on a linked reprint set, without a reload', async ({ page }) => {
  await registerViaApi(page)

  await page.goto('/sets/b1a')
  await expect(page.getByRole('heading', { name: 'Crimson Blaze' })).toBeVisible()

  const firstCard = page.locator('.grid > div').filter({ has: page.locator('img') }).first()
  await firstCard.getByRole('button', { name: 'Add one copy' }).click()
  await expect(firstCard.getByText('×1')).toBeVisible()

  await page.getByRole('link', { name: '← All sets' }).click()
  await expect(page).toHaveURL('/')

  await page.getByRole('link', { name: 'Deluxe Pack: Mega' }).click()
  await expect(page.getByRole('heading', { name: 'Deluxe Pack: Mega' })).toBeVisible()

  const linkedCard = page.locator('.grid > div').filter({ has: page.locator('img') }).first()
  await expect(linkedCard.getByText('×1')).toBeVisible()
})
