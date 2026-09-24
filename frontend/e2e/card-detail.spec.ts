import { expect, test } from '@playwright/test'
import { registerViaApi } from './testUsers'

// Expected values are read off the pinned pokemon-tcg-pocket-cards v5.3.1 dataset.

test('the info button opens the card page without changing the collection, and Back returns to the set', async ({ page }) => {
  await registerViaApi(page)
  await page.goto('/sets/a1')
  await expect(page.getByRole('heading', { name: 'Genetic Apex' })).toBeVisible()

  const firstCard = page.locator('.grid > div').filter({ has: page.locator('img') }).first()
  const badges = firstCard.getByText(/×\d+/)
  const ownedBefore = await badges.count()

  await page.getByRole('link', { name: 'Details for Bulbasaur' }).first().click()
  await expect(page).toHaveURL(/\/cards\/a1-001$/)
  await expect(page.getByRole('heading', { name: 'Bulbasaur', level: 1 })).toBeVisible()
  await expect(page.getByText('Basic', { exact: false }).first()).toBeVisible()
  await expect(page.getByRole('term').filter({ hasText: 'HP' })).toBeVisible()
  await expect(page.getByText('70', { exact: true })).toBeVisible()
  await expect(page.getByText('Weakness')).toBeVisible()
  // Weakness is shown as an energy icon rather than a word.
  await expect(page.getByRole('img', { name: 'Fire energy' })).toBeVisible()

  const attacks = page.getByRole('region', { name: 'Attacks' })
  await expect(attacks.getByText('Vine Whip')).toBeVisible()
  await expect(attacks.getByText('40', { exact: true })).toBeVisible()
  await expect(attacks.getByLabel('Cost: Grass, Colorless')).toBeVisible()
  await expect(page.getByText('Narumi Sato')).toBeVisible()

  await page.getByRole('button', { name: 'Back' }).click()
  await expect(page.getByRole('heading', { name: 'Genetic Apex' })).toBeVisible()
  // Opening details is navigation only — it must not count as a click on the card.
  await expect(badges).toHaveCount(ownedBefore)
})

test('abilities and trainer text are shown, with energy tokens drawn as icons', async ({ page }) => {
  await registerViaApi(page)

  await page.goto('/cards/a1-007')
  await expect(page.getByRole('heading', { name: 'Butterfree', level: 1 })).toBeVisible()
  const ability = page.getByRole('region', { name: 'Ability' })
  await expect(ability.getByText('Powder Heal')).toBeVisible()
  await expect(ability.getByText('Once during your turn, you may heal 20 damage from each of your Pokémon.')).toBeVisible()
  await expect(page.getByRole('region', { name: 'Attacks' }).getByText('Gust')).toBeVisible()

  await page.goto('/cards/a1-216')
  await expect(page.getByRole('heading', { name: 'Helix Fossil', level: 1 })).toBeVisible()
  const text = page.getByRole('region', { name: 'Card text' })
  await expect(text).toContainText('Play this card as if it were a 40-HP Basic')
  await expect(text.getByRole('img', { name: 'Colorless energy' })).toBeVisible()
})

test('an alt-art print shows its base card\'s battle info and links to its other prints', async ({ page }) => {
  await registerViaApi(page)
  await page.goto('/cards/a1-227')

  await expect(page.getByRole('heading', { name: 'Bulbasaur', level: 1 })).toBeVisible()
  await expect(page.getByRole('region', { name: 'Attacks' }).getByText('Vine Whip')).toBeVisible()

  await page.getByRole('region', { name: 'Other prints' }).getByRole('link', { name: /^a1-001/ }).click()
  await expect(page).toHaveURL(/\/cards\/a1-001$/)
  await expect(page.getByRole('heading', { name: 'Bulbasaur', level: 1 })).toBeVisible()
})

test('an unknown card id shows a not-found message', async ({ page }) => {
  await registerViaApi(page)
  await page.goto('/cards/zz-999')

  await expect(page.getByText('Card not found')).toBeVisible()
})
