import { expect, test } from '@playwright/test'

// The manifest and service worker only exist in a production build (they are off in `vite dev`),
// so these run against the Docker frontend instead:
//   E2E_BASE_URL=http://localhost:8081 E2E_PWA=1 npx playwright test e2e/pwa.spec.ts
test.skip(!process.env.E2E_PWA, 'needs a production build; set E2E_PWA=1 and E2E_BASE_URL')

test('the web app manifest is valid and every icon it points to exists', async ({ page, request }) => {
  await page.goto('/login')

  const manifestHref = await page.locator('link[rel="manifest"]').getAttribute('href')
  expect(manifestHref).toBeTruthy()
  const response = await request.get(manifestHref!)
  expect(response.headers()['content-type']).toContain('application/manifest+json')

  const manifest = await response.json()
  expect(manifest).toMatchObject({
    name: 'Pokémon TCG Pocket Collection Tracker',
    short_name: 'PTCGP Tracker',
    display: 'standalone',
    start_url: '/',
    scope: '/',
  })
  expect(manifest.icons.map((icon: { sizes: string }) => icon.sizes)).toEqual(expect.arrayContaining(['192x192', '512x512']))
  expect(manifest.icons.some((icon: { purpose?: string }) => icon.purpose === 'maskable')).toBe(true)
  for (const icon of manifest.icons) {
    const iconResponse = await request.get(`/${icon.src}`)
    expect(iconResponse.status(), icon.src).toBe(200)
    expect(iconResponse.headers()['content-type']).toBe('image/png')
  }

  // iOS ignores manifest icons and reads this tag for "Add to Home Screen".
  const appleIcon = await page.locator('link[rel="apple-touch-icon"]').getAttribute('href')
  expect((await request.get(appleIcon!)).status()).toBe(200)
})

test('Chrome reports no reasons the app can\'t be installed', async ({ page, context }) => {
  await page.goto('/login')
  await page.evaluate(() => navigator.serviceWorker.ready)

  const cdp = await context.newCDPSession(page)
  const { installabilityErrors } = await cdp.send('Page.getInstallabilityErrors')
  expect(installabilityErrors).toEqual([])
})

test('the service worker takes control and never caches API responses', async ({ page }) => {
  await page.goto('/login')
  await page.evaluate(() => navigator.serviceWorker.ready)
  await page.reload()
  await expect.poll(() => page.evaluate(() => Boolean(navigator.serviceWorker.controller))).toBe(true)

  // Give the page an API response to (not) cache, then look at everything the worker stored.
  await page.evaluate(() => fetch('/api/config'))
  const cachedPaths = await page.evaluate(async () => {
    const paths: string[] = []
    for (const name of await caches.keys()) {
      for (const request of await (await caches.open(name)).keys()) paths.push(new URL(request.url).pathname)
    }
    return paths
  })
  expect(cachedPaths).toContain('/index.html')
  expect(cachedPaths.filter(path => path.startsWith('/api'))).toEqual([])
})

test('opened with no network, the installed app shows a retry screen instead of a blank page', async ({ page, context }) => {
  await page.goto('/login')
  await page.evaluate(() => navigator.serviceWorker.ready)
  await page.reload()
  await expect.poll(() => page.evaluate(() => Boolean(navigator.serviceWorker.controller))).toBe(true)

  await context.setOffline(true)
  await page.goto('/')
  // The shell itself comes from the service worker's cache; only the API is unreachable.
  await expect(page.getByRole('heading', { name: 'Can\'t reach the server' })).toBeVisible()

  await context.setOffline(false)
  await page.getByRole('button', { name: 'Try again' }).click()
  await expect(page.getByRole('button', { name: 'Log in' })).toBeVisible()
})
