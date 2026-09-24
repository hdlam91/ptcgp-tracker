import { expect, test } from '@playwright/test'
import { registerViaApi } from './testUsers'

test('searching and filtering narrows the card grid in a set', async ({ page }) => {
  await registerViaApi(page)

  await page.goto('/sets/a1')
  await expect(page.getByRole('heading', { name: 'Genetic Apex' })).toBeVisible()
  await expect(page.getByText('286 / 286 cards')).toBeVisible()

  await page.getByPlaceholder('Search name, number, attack, ability').fill('charizard')
  const matchedCards = page.locator('.grid > div').filter({ has: page.locator('img') })
  await expect(matchedCards).not.toHaveCount(286)
  await expect(matchedCards.first()).toBeVisible()
  for (const name of await matchedCards.locator('p.font-medium').allInnerTexts()) {
    expect(name.toLowerCase()).toContain('charizard')
  }

  await page.getByRole('button', { name: 'Clear' }).click()
  await expect(page.getByText('286 / 286 cards')).toBeVisible()

  const firstCard = page.locator('.grid > div').filter({ has: page.locator('img') }).first()
  await firstCard.getByRole('button').nth(1).click()
  await expect(firstCard.getByText('1', { exact: true })).toBeVisible()

  await page.getByRole('button', { name: 'Owned', exact: true }).click()
  await expect(page.getByText('1 / 286 cards')).toBeVisible()

  await page.getByRole('button', { name: 'Missing', exact: true }).click()
  await expect(page.getByText('285 / 286 cards')).toBeVisible()

  await page.getByRole('button', { name: 'All', exact: true }).click()
  await expect(page.getByText('286 / 286 cards')).toBeVisible()
})

test('filtering to nothing shows an empty state instead of an empty grid', async ({ page }) => {
  await registerViaApi(page)
  await page.goto('/sets/a1')
  await expect(page.getByRole('heading', { name: 'Genetic Apex' })).toBeVisible()

  await page.getByPlaceholder('Search name, number, attack, ability').fill('this card does not exist')
  await expect(page.getByText('No cards match these filters.')).toBeVisible()
})
