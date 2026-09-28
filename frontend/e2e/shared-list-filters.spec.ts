import { expect, test } from '@playwright/test'
import { registerViaApi } from './testUsers'

const csrf = { 'X-Requested-With': 'XMLHttpRequest' }

// a1 = Genetic Apex, a1a = Mythical Island, a2 = Space-Time Smackdown.
// Rarities from the pinned dataset: a1-001 ◊, a1-002 ◊◊, a1-227 ☆, a1a-001 ◊, a1a-002 ◊◊, a2-001 ◊.
const WANTS = ['a1-001', 'a1-002', 'a1-227', 'a1a-001', 'a2-001']
const OFFERS = ['a1-002', 'a1a-002']

// registerViaApi reuses a shared login-mode account rather than a fresh one when self-registration
// is closed, so each test starts by clearing whatever the account's trade list already has —
// otherwise counts here depend on what an earlier test (or run) happened to leave behind.
async function clearTradeList(page: import('@playwright/test').Page) {
  const entries: { cardId: string, direction: string }[] = await (await page.request.get('/api/trade-list')).json()
  for (const entry of entries) {
    await page.request.delete(`/api/trade-list/${entry.cardId}/${entry.direction}`, { headers: csrf })
  }
}

async function shareList(page: import('@playwright/test').Page, wants: string[], offers: string[]) {
  for (const cardId of wants) await page.request.post('/api/trade-list', { headers: csrf, data: { cardId, direction: 'Want' } })
  for (const cardId of offers) await page.request.post('/api/trade-list', { headers: csrf, data: { cardId, direction: 'Offer' } })
  const { handle } = await (await page.request.post('/api/trade-list/share', { headers: csrf })).json()
  return handle as string
}

// The rarity filter is a custom icon dropdown, not a native <select> — each tier's option
// carries a plain-English aria-label (e.g. "2 diamonds") even though it renders as icons only.
async function rarityOptionLabels(page: import('@playwright/test').Page) {
  await page.getByLabel('Filter by rarity').click()
  const labels = await page.locator('#rarity-listbox [role="option"]').evaluateAll(
    els => els.map(el => el.getAttribute('aria-label') ?? el.textContent?.trim() ?? ''),
  )
  await page.keyboard.press('Escape')
  return labels
}

async function selectRarity(page: import('@playwright/test').Page, label: string) {
  await page.getByLabel('Filter by rarity').click()
  await page.getByRole('option', { name: label, exact: true }).click()
}

test('both tabs of a shared list can be filtered by set and rarity, and nothing else', async ({ page, browser }) => {
  await registerViaApi(page)
  await clearTradeList(page)
  const handle = await shareList(page, WANTS, OFFERS)

  // A friend opening the link, with no account.
  const visitor = await browser.newContext()
  const shared = await visitor.newPage()
  await shared.goto(`/share/${handle}`)
  await expect(shared.getByText('5 / 5 cards')).toBeVisible()

  // Only the two requested filters: none of the full filter bar's extras.
  await expect(shared.getByLabel('Filter by set')).toBeVisible()
  await expect(shared.getByLabel('Filter by rarity')).toBeVisible()
  await expect(shared.getByPlaceholder(/Search/)).toHaveCount(0)
  await expect(shared.getByRole('button', { name: 'Filters' })).toHaveCount(0)
  await expect(shared.getByLabel('Filter by pack')).toHaveCount(0)

  const setSelect = shared.getByLabel('Filter by set')
  const raritySelect = shared.getByLabel('Filter by rarity')
  const optionTexts = async (select: import('@playwright/test').Locator) =>
    (await select.locator('option').allTextContents()).map(text => text.trim())

  // Options come from the cards actually on this tab.
  expect(await optionTexts(setSelect)).toEqual(expect.arrayContaining(['Any set', 'Genetic Apex', 'Mythical Island', 'Space-Time Smackdown']))
  expect(await rarityOptionLabels(shared)).toEqual(['Any rarity', '1 diamond', '2 diamonds', '1 star'])

  await setSelect.selectOption({ label: 'Genetic Apex' })
  await expect(shared.getByText('3 / 5 cards')).toBeVisible()
  await selectRarity(shared, '1 diamond')
  await expect(shared.getByText('1 / 5 cards')).toBeVisible()

  await shared.getByRole('button', { name: 'Clear' }).click()
  await expect(shared.getByText('5 / 5 cards')).toBeVisible()
  await selectRarity(shared, '1 diamond')
  await expect(shared.getByText('3 / 5 cards')).toBeVisible()

  // The Offers tab has its own options and starts unfiltered.
  await shared.getByRole('button', { name: 'Offers (2)' }).click()
  await expect(shared.getByText('2 / 2 cards')).toBeVisible()
  await expect(raritySelect).toHaveAttribute('aria-label', /Any rarity/)
  await expect(setSelect).toBeVisible()
  await expect(raritySelect).toBeVisible()
  expect(await optionTexts(setSelect)).toEqual(['Any set', 'Genetic Apex', 'Mythical Island'])
  expect(await rarityOptionLabels(shared)).toEqual(['Any rarity', '2 diamonds'])

  await setSelect.selectOption({ label: 'Mythical Island' })
  await expect(shared.getByText('1 / 2 cards')).toBeVisible()

  // Back on Wants, the Offers selection doesn't linger.
  await shared.getByRole('button', { name: 'Wants (5)' }).click()
  await expect(shared.getByText('5 / 5 cards')).toBeVisible()
  await expect(setSelect).toHaveValue('')

  await selectRarity(shared, '1 star')
  await setSelect.selectOption({ label: 'Space-Time Smackdown' })
  await expect(shared.getByText('No cards match these filters.')).toBeVisible()
  await visitor.close()
})

test('a long shared list is shown a page at a time', async ({ page, browser }) => {
  await registerViaApi(page)
  await clearTradeList(page)
  const many = Array.from({ length: 70 }, (_, index) => `a1-${String(index + 1).padStart(3, '0')}`)
  const handle = await shareList(page, many, [])

  const visitor = await browser.newContext()
  const shared = await visitor.newPage()
  await shared.goto(`/share/${handle}`)
  const cards = shared.locator('.grid > div').filter({ has: shared.locator('img') })

  await expect(shared.getByText('70 / 70 cards')).toBeVisible()
  await expect(cards).toHaveCount(60)
  await shared.getByRole('button', { name: /Show more \(60 \/ 70\)/ }).click()
  await expect(cards).toHaveCount(70)
  await expect(shared.getByRole('button', { name: /Show more/ })).toHaveCount(0)
  await visitor.close()
})
