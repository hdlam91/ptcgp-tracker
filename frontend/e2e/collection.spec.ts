import { expect, test } from '@playwright/test'
import { registerViaApi } from './testUsers'

test('marking a card owned persists across a reload and updates the set progress', async ({ page }) => {
  await registerViaApi(page)

  await page.goto('/sets/a1')
  await expect(page.getByRole('heading', { name: 'Genetic Apex' })).toBeVisible()

  const firstCard = page.locator('.grid > div').filter({ has: page.locator('img') }).first()
  const incrementButton = firstCard.getByRole('button').nth(1)
  await incrementButton.click()
  await incrementButton.click()
  await expect(firstCard.getByText('×2')).toBeVisible()

  await page.reload()
  await expect(page.locator('.grid > div').filter({ has: page.locator('img') }).first().getByText('×2')).toBeVisible()

  await page.getByRole('link', { name: '← All sets' }).click()
  await expect(page).toHaveURL('/')
  await expect(page.getByText('1 / 286')).toBeVisible()
})

test('setting owned count back to zero removes it from the collection', async ({ page }) => {
  await registerViaApi(page)

  await page.goto('/sets/a1')
  await expect(page.getByRole('heading', { name: 'Genetic Apex' })).toBeVisible()

  const firstCard = page.locator('.grid > div').filter({ has: page.locator('img') }).first()
  const [decrementButton, incrementButton] = await firstCard.getByRole('button').all()

  await incrementButton.click()
  await expect(firstCard.getByText('×1')).toBeVisible()

  await decrementButton.click()
  await expect(firstCard.getByText('×1')).not.toBeVisible()
})
