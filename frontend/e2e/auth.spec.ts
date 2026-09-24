import { expect, test } from '@playwright/test'
import { uniqueTestUser } from './testUsers'

test('a visitor can register, stay logged in across a reload, and log out', async ({ page }) => {
  const user = uniqueTestUser('Ash Ketchum')

  await page.goto('/register')
  await page.getByLabel('Display name').fill(user.displayName)
  await page.getByLabel('Email').fill(user.email)
  await page.getByLabel('Password').fill(user.password)
  await page.getByRole('button', { name: 'Sign up' }).click()

  await expect(page).toHaveURL('/')
  await expect(page.getByRole('heading', { name: 'Your sets' })).toBeVisible()
  await expect(page.getByText(user.displayName)).toBeVisible()

  // Session should survive a reload (cookie-based auth, not client-only state).
  await page.reload()
  await expect(page.getByRole('heading', { name: 'Your sets' })).toBeVisible()

  await page.getByRole('button', { name: 'Log out' }).click()
  await expect(page).toHaveURL(/\/login/)

  // A protected route should bounce back to login once logged out.
  await page.goto('/')
  await expect(page).toHaveURL(/\/login/)
})

test('logging in with the wrong password shows an error and does not navigate away', async ({ page }) => {
  const user = uniqueTestUser()
  await page.goto('/register')
  await page.getByLabel('Display name').fill(user.displayName)
  await page.getByLabel('Email').fill(user.email)
  await page.getByLabel('Password').fill(user.password)
  await page.getByRole('button', { name: 'Sign up' }).click()
  await expect(page).toHaveURL('/')

  await page.getByRole('button', { name: 'Log out' }).click()
  await expect(page).toHaveURL(/\/login/)

  await page.getByLabel('Email').fill(user.email)
  await page.getByLabel('Password').fill('WrongPassword1')
  await page.getByRole('button', { name: 'Log in' }).click()

  await expect(page.getByText('Incorrect email or password.')).toBeVisible()
  await expect(page).toHaveURL(/\/login/)
})
