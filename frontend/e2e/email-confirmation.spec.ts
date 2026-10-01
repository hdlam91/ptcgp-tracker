import { expect, test } from '@playwright/test'
import { registerAdminViaApi } from './adminHelper'
import { registerDisposableUser } from './testUsers'

const MAILPIT_URL = process.env.MAILPIT_URL ?? 'http://localhost:8025'

interface MailpitMessageSummary {
  ID: string
  To: { Address: string }[]
}

/** Polls Mailpit for the most recent email to `toEmail` and returns its plain-text body. */
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

async function logOut(page: import('@playwright/test').Page) {
  await page.goto('/')
  await page.getByRole('button', { name: 'Log out' }).click()
  await expect(page).toHaveURL(/\/login$/)
}

// This spec registers accounts of its own and flips a global admin setting, so — like
// account.spec.ts and forgot-password.spec.ts — it always registers genuinely fresh accounts,
// ignoring E2E_LOGIN_EMAIL even when set. It needs registration open on the target backend.
test('requiring email confirmation blocks login until the link is clicked, and resend works', async ({ page, browser }) => {
  await registerAdminViaApi(page)
  await page.goto('/settings')
  await expect(page.getByText('Off — new accounts can log in right away.')).toBeVisible()

  try {
    await page.getByRole('button', { name: 'Require email confirmation' }).click()
    await expect(page.getByText(/Required — new accounts must click a link/)).toBeVisible()

    const visitorContext = await browser.newContext()
    const visitor = await visitorContext.newPage()
    // With confirmation required, registering doesn't sign in — nothing to log out of.
    const { email, password } = await registerDisposableUser(visitor)
    await visitor.goto('/login')
    await visitor.getByLabel('Email').fill(email)
    await visitor.getByLabel('Password').fill(password)
    await visitor.getByRole('button', { name: 'Log in' }).click()
    await expect(visitor.getByRole('heading', { name: 'Confirm your email' })).toBeVisible()
    await expect(visitor.getByText(`You need to confirm ${email}`)).toBeVisible()

    // Resend from the login page produces a second, still-usable link.
    await visitor.getByRole('button', { name: 'Resend confirmation email' }).click()
    await expect(visitor.getByText(/Check your inbox for the confirmation link/)).toBeVisible()

    const body = await waitForEmail(email)
    const confirmLink = body.match(/https?:\/\/\S+/)?.[0]
    if (!confirmLink)
      throw new Error(`No link found in email body: ${body}`)

    await visitor.goto(confirmLink)
    await expect(visitor.getByRole('heading', { name: 'Email confirmed' })).toBeVisible()
    await visitor.getByRole('link', { name: 'Go to the app' }).click()
    await expect(visitor.getByRole('heading', { name: 'Your sets' })).toBeVisible()

    // A fresh login now works normally too, no confirmation step in the way.
    await logOut(visitor)
    await visitor.getByLabel('Email').fill(email)
    await visitor.getByLabel('Password').fill(password)
    await visitor.getByRole('button', { name: 'Log in' }).click()
    await expect(visitor.getByRole('heading', { name: 'Your sets' })).toBeVisible()

    await visitorContext.close()
  }
  finally {
    await page.getByRole('button', { name: 'Turn off email confirmation' }).click()
    await expect(page.getByText('Off — new accounts can log in right away.')).toBeVisible()
  }
})

test('an account that existed before the setting was turned on is never blocked', async ({ page, browser }) => {
  const existingContext = await browser.newContext()
  const existingPage = await existingContext.newPage()
  const { email, password } = await registerDisposableUser(existingPage)
  await existingContext.close()

  await registerAdminViaApi(page)
  await page.goto('/settings')

  try {
    await page.getByRole('button', { name: 'Require email confirmation' }).click()
    await expect(page.getByText(/Required — new accounts must click a link/)).toBeVisible()

    const visitorContext = await browser.newContext()
    const visitor = await visitorContext.newPage()
    await visitor.goto('/login')
    await visitor.getByLabel('Email').fill(email)
    await visitor.getByLabel('Password').fill(password)
    await visitor.getByRole('button', { name: 'Log in' }).click()
    await expect(visitor.getByRole('heading', { name: 'Your sets' })).toBeVisible()
    await visitorContext.close()
  }
  finally {
    await page.getByRole('button', { name: 'Turn off email confirmation' }).click()
    await expect(page.getByText('Off — new accounts can log in right away.')).toBeVisible()
  }
})
