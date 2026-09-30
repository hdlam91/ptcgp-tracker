import { expect, test } from '@playwright/test'
import { registerDisposableUser } from './testUsers'

const MAILPIT_URL = process.env.MAILPIT_URL ?? 'http://localhost:8025'

interface MailpitMessageSummary {
  ID: string
  To: { Address: string }[]
}

/**
 * Polls Mailpit (the fake SMTP catcher `docker-compose.yml` runs for local dev) for the most
 * recent email to `toEmail`, and returns its plain-text body.
 */
async function waitForEmail(toEmail: string): Promise<string> {
  for (let attempt = 0; attempt < 20; attempt++) {
    const list = await (await fetch(`${MAILPIT_URL}/api/v1/messages`)).json() as { messages: MailpitMessageSummary[] }
    const match = list.messages.find(m => m.To.some(to => to.Address === toEmail))
    if (match) {
      const message = await (await fetch(`${MAILPIT_URL}/api/v1/message/${match.ID}`)).json() as { Text: string }
      return message.Text
    }
    await new Promise(resolve => setTimeout(resolve, 250))
  }
  throw new Error(`No email arrived for ${toEmail} — is Mailpit running (docker compose up)?`)
}

// This spec changes the account's own password, so — like account.spec.ts — it always registers
// a genuine fresh account, ignoring E2E_LOGIN_EMAIL even when set.
//
// /api/auth/forgot-password is rate-limited to 3 requests per 15 minutes per IP (Program.cs).
// That's per backend process, not per test run — rerunning this spec several times in a row
// against the same long-lived `docker compose` backend can exhaust it and fail with "No email
// arrived"; restart the backend container (or wait) if that happens.
test('requesting a password reset emails a working link that logs you in with the new password', async ({ page }) => {
  const { email, password } = await registerDisposableUser(page)

  // registerDisposableUser leaves the session logged in; the login page redirects an
  // authenticated visitor away, so log out first.
  await page.goto('/')
  await page.getByRole('button', { name: 'Log out' }).click()
  await expect(page).toHaveURL(/\/login$/)

  await page.getByRole('link', { name: 'Forgot password?' }).click()
  await expect(page).toHaveURL(/\/forgot-password$/)

  await page.getByLabel('Email').fill(email)
  await page.getByRole('button', { name: 'Send reset link' }).click()
  await expect(page.getByText(/check your inbox/)).toBeVisible()

  const body = await waitForEmail(email)
  const resetLink = body.match(/https?:\/\/\S+/)?.[0]
  if (!resetLink)
    throw new Error(`No link found in email body: ${body}`)

  await page.goto(resetLink)
  await page.locator('#new-password').fill('NewPassword1')
  await page.locator('#confirm-password').fill('NewPassword1')
  await page.getByRole('button', { name: 'Reset password' }).click()
  await expect(page).toHaveURL(/\/login$/)

  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByText('Incorrect email or password.')).toBeVisible()

  await page.getByLabel('Password').fill('NewPassword1')
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('heading', { name: 'Your sets' })).toBeVisible()
})

test('requesting a reset for an unregistered email shows the same message, and reveals nothing', async ({ page }) => {
  await page.goto('/forgot-password')
  await page.getByLabel('Email').fill(`nobody-${Date.now()}@example.com`)
  await page.getByRole('button', { name: 'Send reset link' }).click()

  await expect(page.getByText(/check your inbox/)).toBeVisible()
})
