import { expect, test } from '@playwright/test'
import { registerViaApi } from './testUsers'

test('clicking the card image increments owned count, right-clicking decrements it', async ({ page }) => {
  await registerViaApi(page)
  await page.goto('/sets/a1')
  await expect(page.getByRole('heading', { name: 'Genetic Apex' })).toBeVisible()

  const firstCard = page.locator('.grid > div').filter({ has: page.locator('img') }).first()
  const image = firstCard.locator('img')

  await image.click()
  await expect(firstCard.getByText('1', { exact: true })).toBeVisible()

  await image.click()
  await expect(firstCard.getByText('×2')).toBeVisible()

  await image.click({ button: 'right' })
  await expect(firstCard.getByText('1', { exact: true })).toBeVisible()

  await image.click({ button: 'right' })
  await expect(firstCard.locator('span').filter({ hasText: /^×\d+$/ })).not.toBeVisible()

  // Doesn't go negative — the decrement button (and by extension, right-click) is a no-op at 0.
  await image.click({ button: 'right' })
  await expect(firstCard.getByRole('button').first()).toBeDisabled()
})

test('image click/right-click are disabled on the read-only shared trade list', async ({ page }) => {
  await registerViaApi(page)
  await page.goto('/sets/a1')
  const firstCard = page.locator('.grid > div').filter({ has: page.locator('img') }).first()
  await firstCard.locator('img').click()
  await expect(firstCard.getByText('1', { exact: true })).toBeVisible()
  await firstCard.getByTitle('Offer this card for trade').click()

  const enableRes = await page.request.post('/api/trade-list/share', { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
  const { handle } = await enableRes.json()

  const errors: string[] = []
  page.on('pageerror', err => errors.push(err.message))

  await page.goto(`/share/${handle}`)
  await page.getByRole('button', { name: /^Offers/ }).click()
  const sharedCard = page.locator('.grid > div').filter({ has: page.locator('img') }).first()
  await expect(sharedCard).toBeVisible()

  // Clicking/right-clicking the image on a readonly view must not throw or add controls.
  await sharedCard.locator('img').click()
  await sharedCard.locator('img').click({ button: 'right' })
  await expect(sharedCard.getByRole('button')).toHaveCount(0)
  expect(errors).toEqual([])
})
