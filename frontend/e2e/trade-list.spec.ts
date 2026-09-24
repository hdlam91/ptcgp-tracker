import { expect, test } from '@playwright/test'
import { registerViaApi } from './testUsers'

test('adding and removing a card from the want and offer lists', async ({ page }) => {
  await registerViaApi(page)

  await page.goto('/sets/a1')
  const cards = page.locator('.grid > div').filter({ has: page.locator('img') })
  const wantedCardName = await cards.nth(1).locator('p').first().innerText()
  const offeredCardName = await cards.nth(2).locator('p').first().innerText()

  await cards.nth(1).getByTitle('Want this card').click()
  await cards.nth(2).getByTitle('Offer this card for trade').click()

  await page.goto('/trade-list')
  await expect(page.getByRole('button', { name: 'Want (1)' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Offer (1)' })).toBeVisible()

  await expect(page.getByText(wantedCardName, { exact: true })).toBeVisible()

  await page.getByRole('button', { name: 'Offer (1)' }).click()
  await expect(page.getByText(offeredCardName, { exact: true })).toBeVisible()

  // Remove the offered card by toggling it again.
  await page.locator('.grid > div').filter({ has: page.locator('img') }).first()
    .getByTitle('Offer this card for trade').click()
  await expect(page.getByRole('button', { name: 'Offer (0)' })).toBeVisible()
  await expect(page.getByText('No cards here yet.')).toBeVisible()
})
