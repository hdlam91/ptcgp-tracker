import { expect, test } from '@playwright/test'
import { registerViaApi } from './testUsers'

test('sharing a trade list produces a working public read-only link that revokes cleanly', async ({ page, browser }) => {
  const user = await registerViaApi(page)

  await page.goto('/sets/a1')
  await page.waitForSelector('img')
  const cards = page.locator('.grid > div').filter({ has: page.locator('img') })
  await cards.nth(0).getByTitle('Want this card').click()

  await page.goto('/trade-list')
  await page.getByRole('button', { name: 'Create share link' }).click()
  const shareUrl = await page.locator('input[readonly]').inputValue()
  // Short, readable link built from the display name — "Test Trainer" → /share/test-trainer[-N].
  expect(shareUrl).toMatch(/\/share\/test-trainer(-\d+)?$/)

  // A completely separate, unauthenticated browser context — like a friend clicking the link.
  const anonContext = await browser.newContext()
  const anonPage = await anonContext.newPage()
  await anonPage.goto(shareUrl)
  await expect(anonPage.getByRole('heading', { name: `${user.displayName}'s trade list` })).toBeVisible()
  await expect(anonPage.getByRole('button', { name: 'Wants (1)' })).toBeVisible()
  // Read-only: no owned-count controls or trade toggles should be present.
  await expect(anonPage.getByTitle('Want this card')).toHaveCount(0)
  await anonContext.close()

  await page.getByRole('button', { name: 'Stop sharing' }).click()
  await expect(page.getByRole('button', { name: 'Create share link' })).toBeVisible()

  const revokedContext = await browser.newContext()
  const revokedPage = await revokedContext.newPage()
  await revokedPage.goto(shareUrl)
  await expect(revokedPage.getByText('invalid or no longer active')).toBeVisible()
  await revokedContext.close()
})
