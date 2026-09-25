import { expect, test } from '@playwright/test'
import type { Page } from '@playwright/test'
import { registerViaApi } from './testUsers'

// The header menu collapses into a hamburger only when it no longer fits, so instead of pinning a
// screen width these check the rule itself at many widths: one mode or the other, never both, and
// never a clipped inline menu.
async function menuIsConsistent(page: Page): Promise<boolean> {
  const hamburger = page.getByRole('button', { name: /^(Open|Close) menu$/ })
  const inlineNav = page.getByRole('navigation')
  if (await hamburger.count() > 0) {
    // Collapsed: no inline menu. (The dropdown, when open, is the only navigation; the invisible copy
    // used for measuring is hidden from the accessibility tree, so it never shows up here.)
    return (await inlineNav.count()) === 0 || (await hamburger.getAttribute('aria-expanded')) === 'true'
  }
  // Inline: it must all be there and unclipped.
  const links = inlineNav.getByRole('link')
  if (await links.count() < 3) return false
  return inlineNav.evaluate(nav => nav.scrollWidth <= nav.clientWidth)
}

test('the menu is inline while it fits and a hamburger when it does not, at every width', async ({ page }) => {
  await registerViaApi(page)
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Your sets' })).toBeVisible()

  const seen = { inline: false, hamburger: false }
  for (const width of [1400, 1024, 800, 700, 620, 560, 500, 440, 400, 360, 320]) {
    await page.setViewportSize({ width, height: 800 })
    await expect.poll(() => menuIsConsistent(page), { message: `header at ${width}px` }).toBe(true)
    if (await page.getByRole('button', { name: 'Open menu' }).count() > 0) seen.hamburger = true
    else seen.inline = true
  }
  // Both modes really were exercised across that range.
  expect(seen).toEqual({ inline: true, hamburger: true })
})

test('the hamburger opens a menu with every page, closes on navigating, Escape and outside clicks', async ({ page }) => {
  await registerViaApi(page)
  await page.setViewportSize({ width: 340, height: 800 })
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Your sets' })).toBeVisible()

  const open = page.getByRole('button', { name: 'Open menu' })
  await expect(open).toBeVisible()
  await expect(open).toHaveAttribute('aria-expanded', 'false')
  await expect(page.getByRole('link', { name: 'Trade list' })).toHaveCount(0)
  // The right-hand actions live in the menu too, not in the header row.
  await expect(page.getByRole('button', { name: 'Log out' })).toHaveCount(0)
  await expect(page.getByRole('button', { name: /Switch to (dark|light) mode/ })).toHaveCount(0)

  await open.click()
  const menu = page.locator('#app-menu')
  await expect(menu.getByRole('link', { name: 'Collection' })).toBeVisible()
  await expect(menu.getByRole('link', { name: 'All cards' })).toBeVisible()
  await expect(menu.getByRole('link', { name: 'Trade list' })).toBeVisible()
  await expect(menu.getByRole('link', { name: 'Collection' })).toHaveAttribute('aria-current', 'page')
  await expect(page.getByRole('button', { name: 'Close menu' })).toHaveAttribute('aria-expanded', 'true')

  // Theme switch works from the menu and leaves it open.
  const wasDark = await page.evaluate(() => document.documentElement.classList.contains('dark'))
  await menu.getByRole('button', { name: /Switch to (dark|light) mode/ }).click()
  expect(await page.evaluate(() => document.documentElement.classList.contains('dark'))).toBe(!wasDark)
  await expect(menu).toBeVisible()

  // Picking a page goes there and closes the menu.
  await menu.getByRole('link', { name: 'Trade list' }).click()
  await expect(page).toHaveURL(/\/trade-list$/)
  await expect(menu).toHaveCount(0)

  await page.getByRole('button', { name: 'Open menu' }).click()
  await expect(menu).toBeVisible()
  await page.keyboard.press('Escape')
  await expect(menu).toHaveCount(0)

  await page.getByRole('button', { name: 'Open menu' }).click()
  await expect(menu).toBeVisible()
  // The dropdown covers the top of the page, so click a blank spot lower down.
  await page.mouse.click(170, 700)
  await expect(menu).toHaveCount(0)

  // Widening the window puts the menu back inline and drops the dropdown.
  await page.getByRole('button', { name: 'Open menu' }).click()
  await expect(menu).toBeVisible()
  await page.setViewportSize({ width: 1280, height: 800 })
  await expect(page.getByRole('button', { name: /menu$/ })).toHaveCount(0)
  await expect(menu).toHaveCount(0)
  await expect(page.getByRole('navigation').getByRole('link', { name: 'Trade list' })).toBeVisible()
})

test('logging out from the hamburger menu returns to the login page', async ({ page }) => {
  await registerViaApi(page)
  await page.setViewportSize({ width: 340, height: 800 })
  await page.goto('/')
  await page.getByRole('button', { name: 'Open menu' }).click()
  await page.locator('#app-menu').getByRole('button', { name: 'Log out' }).click()
  await expect(page).toHaveURL(/\/login$/)
})
