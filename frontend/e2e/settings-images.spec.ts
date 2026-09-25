import { expect, test } from '@playwright/test'
import { registerAdminViaApi } from './adminHelper'

// The real download pulls ~135 MB from GitHub, so these mock the two admin endpoints and check what
// the page shows at each stage. (The server side is covered by the backend tests.)
const idle = { total: 0, completed: 0, failed: 0, startedAt: null, finishedAt: null, storedCards: 0, storedPacks: 0, storedBytes: 0, error: null }

test('an admin can start the image download and watch it progress to the end', async ({ page }) => {
  let status = { ...idle, state: 'Idle' }
  await page.route('**/api/admin/images', route => route.fulfill({ json: status }))
  await page.route('**/api/admin/images/download', (route) => {
    status = { ...idle, state: 'Running', total: 100, completed: 40 }
    return route.fulfill({ status: 202, json: status })
  })

  await registerAdminViaApi(page)
  await page.goto('/settings')
  await expect(page.getByText('No images are stored on this server yet.')).toBeVisible()

  await page.getByRole('button', { name: 'Download card images' }).click()
  await expect(page.getByText('Downloading… 40 of 100')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Downloading…' })).toBeDisabled()

  // The page keeps polling; the server later reports it done, with a couple of misses.
  status = { ...idle, state: 'Completed', total: 100, completed: 98, failed: 2, storedCards: 90, storedPacks: 8, storedBytes: 141_557_760 }
  await expect(page.getByText('Finished: 98 of 100 images are stored.')).toBeVisible({ timeout: 8000 })
  await expect(page.getByText('2 couldn\'t be downloaded. Run it again to retry just those.')).toBeVisible()
  await expect(page.getByText('90 card images and 8 pack images stored here (135.0 MB).')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Download missing images' })).toBeEnabled()
})

test('a download already running when the page opens is shown, and a failed one says why', async ({ page }) => {
  let status = { ...idle, state: 'Running', total: 10, completed: 3 }
  await page.route('**/api/admin/images', route => route.fulfill({ json: status }))

  await registerAdminViaApi(page)
  await page.goto('/settings')
  await expect(page.getByText('Downloading… 3 of 10')).toBeVisible()

  status = { ...idle, state: 'Failed', error: 'The card data hasn\'t loaded yet.' }
  await expect(page.getByText('The download stopped: The card data hasn\'t loaded yet.')).toBeVisible({ timeout: 8000 })
  await expect(page.getByRole('button', { name: 'Download card images' })).toBeEnabled()
})
