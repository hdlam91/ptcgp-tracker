import { expect, test } from '@playwright/test'

test('when the server can\'t be reached, a retry screen replaces a blank page, and Try again recovers', async ({ page }) => {
  // Same failure an offline phone sees: the startup "who am I?" request never gets an answer.
  await page.route('**/api/auth/me', route => route.abort('internetdisconnected'))

  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Can\'t reach the server' })).toBeVisible()
  // Not mistaken for "logged out": no redirect to the login page.
  await expect(page).not.toHaveURL(/login/)

  await page.unroute('**/api/auth/me')
  await page.getByRole('button', { name: 'Try again' }).click()
  await expect(page.getByRole('button', { name: 'Log in' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Can\'t reach the server' })).toHaveCount(0)
})

test('a server error at startup is treated the same way', async ({ page }) => {
  await page.route('**/api/auth/me', route => route.fulfill({ status: 502, body: 'Bad Gateway' }))

  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Can\'t reach the server' })).toBeVisible()
})
