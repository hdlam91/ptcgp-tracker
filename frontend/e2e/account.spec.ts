import crypto from 'node:crypto'
import { expect, test } from '@playwright/test'
import type { Page } from '@playwright/test'

// These specs change or destroy the very credentials a login-mode E2E_LOGIN_EMAIL account would
// need (password, email, 2FA, or the account itself), so — unlike most specs here — they always
// register a genuine fresh account directly, ignoring the login-mode env vars other specs use.
async function registerFreshUser(page: Page) {
  const email = `acct-${Date.now()}-${Math.random().toString(36).slice(2)}@example.com`
  const password = 'Password1'
  const response = await page.request.post('/api/auth/register', {
    headers: { 'X-Requested-With': 'XMLHttpRequest' },
    data: { email, password, displayName: 'Acct Tester' },
  })
  if (!response.ok()) {
    throw new Error(`Failed to register a fresh account for an account-settings test: ${response.status()} ${await response.text()}. `
      + 'These specs need registration open on the target backend.')
  }
  await page.goto('/account')
  return { email, password }
}

function base32Decode(input: string): Buffer {
  const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567'
  let bits = ''
  for (const char of input.replace(/=+$/, '').toUpperCase()) {
    const value = alphabet.indexOf(char)
    if (value === -1) continue
    bits += value.toString(2).padStart(5, '0')
  }
  const bytes: number[] = []
  for (let i = 0; i + 8 <= bits.length; i += 8) bytes.push(Number.parseInt(bits.slice(i, i + 8), 2))
  return Buffer.from(bytes)
}

/** RFC 6238 TOTP, computed the same way an authenticator app would from the setup's shared key. */
function totp(sharedKeyBase32: string): string {
  const key = base32Decode(sharedKeyBase32)
  const counter = Math.floor(Date.now() / 1000 / 30)
  const counterBuffer = Buffer.alloc(8)
  counterBuffer.writeBigUInt64BE(BigInt(counter))
  const hmac = crypto.createHmac('sha1', key).update(counterBuffer).digest()
  const offset = hmac[hmac.length - 1] & 0xf
  const code = ((hmac[offset] & 0x7f) << 24 | (hmac[offset + 1] & 0xff) << 16
    | (hmac[offset + 2] & 0xff) << 8 | (hmac[offset + 3] & 0xff)) % 1_000_000
  return code.toString().padStart(6, '0')
}

/** Same as {@link totp}, but waits out a near-expiring 30s window first, so a slow UI round trip
 * (filling a field, clicking, the request itself) can't land the code on the wrong side of it. */
async function freshTotpCode(sharedKeyBase32: string): Promise<string> {
  const msIntoWindow = Date.now() % 30_000
  if (msIntoWindow > 25_000) {
    await new Promise(resolve => setTimeout(resolve, 30_000 - msIntoWindow + 250))
  }
  return totp(sharedKeyBase32)
}

test('changing your password takes effect immediately, and the old password stops working', async ({ page }) => {
  const { email, password } = await registerFreshUser(page)

  await page.locator('#current-password').fill(password)
  await page.locator('#new-password').fill('NewPassword1')
  await page.locator('#confirm-password').fill('NewPassword1')
  await page.getByRole('button', { name: 'Change password' }).click()
  await expect(page.getByText('Password changed.')).toBeVisible()

  await page.getByRole('button', { name: 'Log out' }).click()
  await page.waitForURL('**/login')

  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByText('Incorrect email or password.')).toBeVisible()

  await page.getByLabel('Password').fill('NewPassword1')
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('heading', { name: 'Your sets' })).toBeVisible()
})

test('changing your email lets you log in with the new address', async ({ page }) => {
  const { password } = await registerFreshUser(page)
  const newEmail = `acct-new-${Date.now()}@example.com`

  await page.locator('#new-email').fill(newEmail)
  await page.locator('#email-current-password').fill(password)
  await page.getByRole('button', { name: 'Change email' }).click()
  await expect(page.getByText('Email changed.')).toBeVisible()
  await expect(page.getByText(`Signed in as ${newEmail}`)).toBeVisible()

  await page.getByRole('button', { name: 'Log out' }).click()
  await page.waitForURL('**/login')
  await page.getByLabel('Email').fill(newEmail)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('heading', { name: 'Your sets' })).toBeVisible()
})

test('deleting your account signs you out and the account can no longer log in', async ({ page }) => {
  const { email, password } = await registerFreshUser(page)

  await page.getByRole('button', { name: 'Delete my account' }).click()
  await page.locator('#delete-current-password').fill('WrongPassword1')
  await page.getByRole('button', { name: 'Yes, delete everything' }).click()
  await expect(page.getByText('Incorrect password.')).toBeVisible()

  await page.locator('#delete-current-password').fill(password)
  await page.getByRole('button', { name: 'Yes, delete everything' }).click()
  await page.waitForURL('**/login')

  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByText('Incorrect email or password.')).toBeVisible()
})

test('enabling two-factor requires a code at the next login, and disabling turns that back off', async ({ page }) => {
  const { email, password } = await registerFreshUser(page)

  await page.getByRole('button', { name: 'Enable two-factor' }).click()
  await page.locator('canvas').waitFor()
  // The QR/key paragraph is in the DOM as soon as the setup step opens, but its text only fills
  // in once the async /2fa/setup response comes back — wait for that, not just the element.
  const sharedKeyLocator = page.locator('p.font-mono')
  await expect(sharedKeyLocator).not.toHaveText('')
  const sharedKey = (await sharedKeyLocator.textContent() ?? '').trim()

  await page.getByLabel('Code').fill('000000')
  await page.getByRole('button', { name: 'Enable', exact: true }).click()
  await expect(page.getByText("isn't valid")).toBeVisible()

  await page.getByLabel('Code').fill(await freshTotpCode(sharedKey))
  await page.getByRole('button', { name: 'Enable', exact: true }).click()
  await expect(page.getByText('Save these recovery codes')).toBeVisible()
  const recoveryCodes = await page.locator('ul.font-mono li').allInnerTexts()
  expect(recoveryCodes.length).toBeGreaterThan(0)
  await page.getByRole('button', { name: 'I\'ve saved these' }).click()
  await expect(page.getByText('Enabled — an authenticator')).toBeVisible()

  await page.getByRole('button', { name: 'Log out' }).click()
  await page.waitForURL('**/login')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('heading', { name: 'Two-factor authentication' })).toBeVisible()

  await page.getByLabel('Code').fill(await freshTotpCode(sharedKey))
  await page.getByRole('button', { name: 'Verify' }).click()
  await expect(page.getByRole('heading', { name: 'Your sets' })).toBeVisible()

  await page.goto('/account')
  await page.getByRole('button', { name: 'Disable' }).click()
  await page.locator('#twofactor-current-password').fill(password)
  await page.getByRole('button', { name: 'Yes, disable' }).click()
  await expect(page.getByText('Off — add an authenticator')).toBeVisible()

  await page.getByRole('button', { name: 'Log out' }).click()
  await page.waitForURL('**/login')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('heading', { name: 'Your sets' })).toBeVisible()
})

test('a recovery code logs in once and then stops working', async ({ page }) => {
  const { email, password } = await registerFreshUser(page)

  await page.getByRole('button', { name: 'Enable two-factor' }).click()
  await page.locator('canvas').waitFor()
  const sharedKeyLocator2 = page.locator('p.font-mono')
  await expect(sharedKeyLocator2).not.toHaveText('')
  const sharedKey = (await sharedKeyLocator2.textContent() ?? '').trim()
  await page.getByLabel('Code').fill(await freshTotpCode(sharedKey))
  await page.getByRole('button', { name: 'Enable', exact: true }).click()
  await expect(page.getByText('Save these recovery codes')).toBeVisible()
  const [recoveryCode] = await page.locator('ul.font-mono li').allInnerTexts()
  await page.getByRole('button', { name: 'I\'ve saved these' }).click()

  await page.getByRole('button', { name: 'Log out' }).click()
  await page.waitForURL('**/login')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Log in' }).click()
  await page.getByRole('button', { name: 'Use a recovery code instead' }).click()
  await page.getByLabel('Recovery code').fill(recoveryCode)
  await page.getByRole('button', { name: 'Verify' }).click()
  await expect(page.getByRole('heading', { name: 'Your sets' })).toBeVisible()

  // Log back in with the same recovery code: it's already spent.
  await page.getByRole('button', { name: 'Log out' }).click()
  await page.waitForURL('**/login')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Log in' }).click()
  await page.getByRole('button', { name: 'Use a recovery code instead' }).click()
  await page.getByLabel('Recovery code').fill(recoveryCode)
  await page.getByRole('button', { name: 'Verify' }).click()
  await expect(page.getByText("recovery code isn't valid")).toBeVisible()
})
