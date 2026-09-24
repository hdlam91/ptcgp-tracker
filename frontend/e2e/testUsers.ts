import type { Page } from '@playwright/test'

export interface TestUser {
  email: string
  password: string
  displayName: string
}

export function uniqueTestUser(displayName = 'Test Trainer'): TestUser {
  return {
    email: `e2e-${Date.now()}-${Math.random().toString(36).slice(2)}@example.com`,
    password: 'Password1',
    displayName,
  }
}

/**
 * Registers a fresh user against the real backend API (not mocked) and leaves
 * the browser context's session cookie set, so the caller can navigate straight
 * into authenticated pages without re-doing the signup form in every test.
 */
export async function registerViaApi(page: Page, user: TestUser = uniqueTestUser()): Promise<TestUser> {
  // An admin can close registration on the dev backend. Set E2E_LOGIN_EMAIL / E2E_LOGIN_PASSWORD
  // to sign in as an existing account instead — only safe for tests that don't change its data.
  const loginEmail = process.env.E2E_LOGIN_EMAIL
  if (loginEmail) {
    const login = await page.request.post('/api/auth/login', {
      headers: { 'X-Requested-With': 'XMLHttpRequest' },
      data: { email: loginEmail, password: process.env.E2E_LOGIN_PASSWORD ?? '' },
    })
    if (!login.ok()) {
      throw new Error(`Failed to log in as ${loginEmail}: ${login.status()}`)
    }
    return { email: loginEmail, password: process.env.E2E_LOGIN_PASSWORD ?? '', displayName: 'Existing account' }
  }

  const response = await page.request.post('/api/auth/register', {
    headers: { 'X-Requested-With': 'XMLHttpRequest' },
    data: user,
  })
  if (!response.ok()) {
    throw new Error(`Failed to register test user: ${response.status()} ${await response.text()}`)
  }
  return user
}
