import { expect, test } from '@playwright/test'
import { registerViaApi } from './testUsers'

// Counts are read off the pinned pokemon-tcg-pocket-cards v5.3.1 dataset: Shining Revelry (a2b) has 14 shiny cards.
test('shiny cards are their own rarity on the collection page and the set page, not counted as star', async ({ page }) => {
  await registerViaApi(page)
  await page.goto('/')
  const setCard = page.getByRole('link', { name: /Shining Revelry/ })
  await expect(setCard.getByText('0/14')).toBeVisible()

  await page.goto('/sets/a2b')
  const shinyPanel = page.locator('div.rounded-lg.border').filter({ hasText: /^\s*Shiny/ })
  await expect(shinyPanel).toContainText('0 / 14')
  await expect(page.locator('div.rounded-lg.border').filter({ hasText: /^\s*Star/ })).not.toContainText('0 / 14')
})
