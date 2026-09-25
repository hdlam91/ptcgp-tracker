import { expect, test } from '@playwright/test'
import { registerViaApi } from './testUsers'

const csrf = { 'X-Requested-With': 'XMLHttpRequest' }

async function shareAList(page: import('@playwright/test').Page) {
  await page.request.post('/api/trade-list', { headers: csrf, data: { cardId: 'a1-001', direction: 'Want' } })
  const { handle } = await (await page.request.post('/api/trade-list/share', { headers: csrf })).json()
  return handle as string
}

test('a logged-in visitor on a shared list gets the app navigation and can reach their own collection', async ({ page }) => {
  await registerViaApi(page)
  const handle = await shareAList(page)

  // Like opening your own share link inside the installed app: no address bar, no back button.
  await page.goto(`/share/${handle}`)
  await expect(page.getByRole('button', { name: /^Wants/ })).toBeVisible()

  const nav = page.getByRole('navigation')
  await expect(nav.getByRole('link', { name: 'Collection' })).toBeVisible()
  await expect(nav.getByRole('link', { name: 'Trade list' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Log in' })).toHaveCount(0)

  await nav.getByRole('link', { name: 'Collection' }).click()
  await expect(page.getByRole('heading', { name: 'Your sets' })).toBeVisible()
})

test('a logged-out visitor keeps the clean read-only page, with a way to log in', async ({ page, browser }) => {
  await registerViaApi(page)
  const handle = await shareAList(page)

  const visitor = await browser.newContext()
  const shared = await visitor.newPage()
  await shared.goto(`/share/${handle}`)
  await expect(shared.getByRole('button', { name: /^Wants/ })).toBeVisible()
  await expect(shared.getByRole('navigation')).toHaveCount(0)

  await shared.getByRole('link', { name: 'Log in' }).click()
  await expect(shared.getByRole('button', { name: 'Log in' })).toBeVisible()

  // A dead or revoked link isn't a dead end either.
  await shared.goto('/share/no-such-link-anywhere')
  await expect(shared.getByText('invalid or no longer active')).toBeVisible()
  await expect(shared.getByRole('link', { name: 'Log in' })).toBeVisible()
  await visitor.close()
})
