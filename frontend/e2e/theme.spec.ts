import { expect, test } from '@playwright/test'

test('toggling dark mode applies immediately and persists across a reload', async ({ page }) => {
  await page.goto('/login')

  const isDark = () => page.evaluate(() => document.documentElement.classList.contains('dark'))

  expect(await isDark()).toBe(false)

  await page.getByTitle('Switch to dark mode').click()
  expect(await isDark()).toBe(true)

  await page.reload()
  expect(await isDark()).toBe(true)

  await page.getByTitle('Switch to light mode').click()
  expect(await isDark()).toBe(false)

  await page.reload()
  expect(await isDark()).toBe(false)
})
