import { expect, test } from '@playwright/test'
import { registerAdminViaApi, registerOtherUser } from './adminHelper'
import { registerViaApi } from './testUsers'

const csrf = { 'X-Requested-With': 'XMLHttpRequest' }

test('non-admins never see the settings link and are sent away from /settings', async ({ page }) => {
  await registerViaApi(page)

  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Your sets' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Settings' })).toHaveCount(0)

  await page.goto('/settings')
  await expect(page.getByRole('heading', { name: 'Your sets' })).toBeVisible()
  await expect(page).not.toHaveURL(/settings/)
})

test('an admin can promote and demote another user, then delete them along with their session', async ({ page, request }) => {
  const other = await registerOtherUser(request)
  await registerAdminViaApi(page)

  await page.goto('/')
  await page.getByRole('link', { name: 'Settings' }).click()
  await expect(page.getByRole('heading', { name: 'Settings' })).toBeVisible()

  const row = page.getByTestId(`user-row-${other.email}`)
  await expect(row).toBeVisible()
  const adminBadge = row.getByText('Admin', { exact: true })
  await expect(adminBadge).toHaveCount(0)

  await row.getByRole('button', { name: 'Make admin' }).click()
  await expect(adminBadge).toBeVisible()
  // The promoted user's existing session gets the role immediately, without logging in again.
  await expect.poll(async () => (await request.get('/api/admin/users')).status()).toBe(200)

  await row.getByRole('button', { name: 'Remove admin' }).click()
  await expect(adminBadge).toHaveCount(0)
  await expect.poll(async () => (await request.get('/api/admin/users')).status()).toBe(403)

  // Deleting takes a confirmation step; cancelling leaves the user alone.
  await row.getByRole('button', { name: 'Delete' }).click()
  await row.getByRole('button', { name: 'Cancel' }).click()
  await expect(row).toBeVisible()

  await row.getByRole('button', { name: 'Delete' }).click()
  await row.getByRole('button', { name: 'Yes, delete' }).click()
  await expect(row).toHaveCount(0)
  await expect.poll(async () => (await request.get('/api/auth/me')).status()).toBe(401)
})

test('admins cannot act on their own row', async ({ page }) => {
  const admin = await registerAdminViaApi(page)

  await page.goto('/settings')
  const ownRow = page.getByTestId(`user-row-${admin.email}`)
  await expect(ownRow).toBeVisible()
  await expect(ownRow.getByText('(you)')).toBeVisible()
  await expect(ownRow.getByRole('button')).toHaveCount(0)
})

test('an admin can close registration, which hides sign-up, and reopen it', async ({ page, browser }) => {
  await registerAdminViaApi(page)
  await page.goto('/settings')
  await expect(page.getByText('Open — anyone can create an account.')).toBeVisible()

  try {
    await page.getByRole('button', { name: 'Close registration' }).click()
    await expect(page.getByText(/Closed — the sign-up page is disabled/)).toBeVisible()

    const visitor = await browser.newContext()
    const visitorPage = await visitor.newPage()
    await visitorPage.goto('/register')
    await expect(visitorPage.getByText('Registration is closed', { exact: true })).toBeVisible()
    await expect(visitorPage.getByLabel('Email')).toHaveCount(0)
    await visitorPage.goto('/login')
    await expect(visitorPage.getByRole('button', { name: 'Log in' })).toBeVisible()
    await expect(visitorPage.getByRole('link', { name: 'Sign up' })).toHaveCount(0)
    await visitor.close()

    await page.getByRole('button', { name: 'Open registration' }).click()
    await expect(page.getByText('Open — anyone can create an account.')).toBeVisible()

    const returning = await browser.newContext()
    const returningPage = await returning.newPage()
    await returningPage.goto('/login')
    await expect(returningPage.getByRole('link', { name: 'Sign up' })).toBeVisible()
    await returning.close()
  }
  finally {
    // Every other test registers users against this same backend — never leave sign-up closed.
    await page.request.put('/api/admin/settings', { headers: csrf, data: { registrationOpen: true } })
  }
})

test('an admin can see and disable a user\'s public share link', async ({ page, request }) => {
  const other = await registerOtherUser(request, 'Sharer')
  const share = await request.post('/api/trade-list/share', { headers: csrf })
  const { handle } = await share.json()
  await registerAdminViaApi(page)

  await page.goto('/settings')
  const row = page.getByTestId(`user-row-${other.email}`)
  await expect(row.getByRole('link', { name: `/share/${handle}` })).toBeVisible()

  await row.getByRole('button', { name: 'Disable' }).click()
  await expect(row.getByRole('link')).toHaveCount(0)

  const publicLookup = await request.get(`/api/trade-list/shared/${handle}`)
  expect(publicLookup.status()).toBe(404)
})

test('the card data panel shows the loaded catalog and can refresh it', async ({ page }) => {
  await registerAdminViaApi(page)
  await page.goto('/settings')

  await expect(page.getByText('Dataset version')).toBeVisible()
  await expect(page.getByText('3879', { exact: true })).toBeVisible()

  await page.getByRole('button', { name: 'Refresh card data' }).click()
  await expect(page.getByRole('button', { name: 'Refresh card data' })).toBeEnabled()
  await expect(page.getByRole('alert')).toHaveCount(0)
  await expect(page.getByText('Never', { exact: true })).toHaveCount(0)
})

test('the users list can be searched by name or email', async ({ page, request }) => {
  const needle = await registerOtherUser(request, 'Findable Trainer')
  await registerOtherUser(request, 'Someone Else')
  await registerAdminViaApi(page)
  await page.goto('/settings')

  const search = page.getByLabel('Search users')
  await search.fill(needle.email)
  await expect(page.getByTestId(`user-row-${needle.email}`)).toBeVisible()
  await expect(page.locator('[data-testid^="user-row-"]')).toHaveCount(1)

  await search.fill('findable trainer')
  await expect(page.getByTestId(`user-row-${needle.email}`)).toBeVisible()

  await search.fill('no-such-user-anywhere')
  await expect(page.getByText('No users match "no-such-user-anywhere".')).toBeVisible()
  await expect(page.locator('[data-testid^="user-row-"]')).toHaveCount(0)
})
