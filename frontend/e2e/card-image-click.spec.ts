import { expect, test } from '@playwright/test'
import { registerViaApi } from './testUsers'

test('the hover +/- buttons change the owned count, and clicking the art opens the card page instead', async ({ page }) => {
  await registerViaApi(page)
  await page.goto('/sets/a1')
  await expect(page.getByRole('heading', { name: 'Genetic Apex' })).toBeVisible()

  const firstCard = page.locator('.grid > div').filter({ has: page.locator('img') }).first()
  const badge = firstCard.getByText(/×\d+/)
  const add = firstCard.getByRole('button', { name: 'Add one copy' })
  const remove = firstCard.getByRole('button', { name: 'Remove one copy' })

  // Nothing owned yet: no badge, and there's nothing to remove.
  await expect(badge).toHaveCount(0)
  await expect(remove).toBeDisabled()

  // The controls only appear while the pointer is over the card; hovering reveals them.
  await firstCard.hover()
  await expect(add).toBeVisible()
  await add.click()
  await expect(badge).toHaveText('×1')
  await add.click()
  await expect(badge).toHaveText('×2')
  await remove.click()
  await expect(badge).toHaveText('×1')
  await remove.click()
  await expect(badge).toHaveCount(0)
  await expect(remove).toBeDisabled()

  // Clicking the art no longer changes the count — it opens the card's page. Right-click does nothing.
  await add.click()
  await expect(badge).toHaveText('×1')
  await firstCard.locator('img').click({ button: 'right' })
  await expect(badge).toHaveText('×1')
  await firstCard.locator('img').click({ position: { x: 60, y: 60 } })
  await expect(page).toHaveURL(/\/cards\/a1-001$/)
  await expect(page.getByRole('heading', { name: 'Bulbasaur', level: 1 })).toBeVisible()
  await expect(page.getByText('You own ×1.')).toBeVisible()
})

test('the -/count/+ box under the card also changes the owned count', async ({ page }) => {
  await registerViaApi(page)
  await page.goto('/sets/a1')
  await expect(page.getByRole('heading', { name: 'Genetic Apex' })).toBeVisible()

  const firstCard = page.locator('.grid > div').filter({ has: page.locator('img') }).first()
  const badge = firstCard.getByText(/×\d+/)
  const count = firstCard.locator('span.tabular-nums')
  const increase = firstCard.getByRole('button', { name: 'Increase owned count' })
  const decrease = firstCard.getByRole('button', { name: 'Decrease owned count' })

  await expect(count).toHaveText('0')
  await expect(decrease).toBeDisabled()

  await increase.click()
  await expect(count).toHaveText('1')
  await expect(badge).toHaveText('×1')
  await increase.click()
  await expect(count).toHaveText('2')
  await decrease.click()
  await expect(count).toHaveText('1')
  await decrease.click()
  await expect(count).toHaveText('0')
  await expect(badge).toHaveCount(0)
  await expect(decrease).toBeDisabled()
})

test('the read-only shared trade list has no +/- controls and its card art is not a link', async ({ page }) => {
  await registerViaApi(page)
  await page.goto('/sets/a1')
  const firstCard = page.locator('.grid > div').filter({ has: page.locator('img') }).first()
  await firstCard.getByRole('button', { name: 'Add one copy' }).click()
  await expect(firstCard.getByText('×1')).toBeVisible()
  await firstCard.getByTitle('Offer this card for trade').click()

  const enableRes = await page.request.post('/api/trade-list/share', { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
  const { handle } = await enableRes.json()

  const errors: string[] = []
  page.on('pageerror', err => errors.push(err.message))

  await page.goto(`/share/${handle}`)
  await page.getByRole('button', { name: /^Offers/ }).click()
  const sharedCard = page.locator('.grid > div').filter({ has: page.locator('img') }).first()
  await expect(sharedCard).toBeVisible()
  const sharedUrl = page.url()

  // Clicking the art on a read-only view must not navigate, throw, or add controls.
  await sharedCard.locator('img').click()
  await sharedCard.locator('img').click({ button: 'right' })
  await expect(page).toHaveURL(sharedUrl)
  await expect(sharedCard.getByRole('button')).toHaveCount(0)
  // The details page needs a login, so the read-only public view doesn't link to it either.
  await expect(sharedCard.getByRole('link')).toHaveCount(0)
  expect(errors).toEqual([])
})
