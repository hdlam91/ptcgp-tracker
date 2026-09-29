import { execFileSync } from 'node:child_process'
import path from 'node:path'
import type { Page } from '@playwright/test'

export interface TestUser {
  email: string
  password: string
  displayName: string
}

const csrfHeaders = { 'X-Requested-With': 'XMLHttpRequest' }

// Playwright runs from frontend/; docker-compose.yml lives one level up.
const repoRoot = path.resolve(process.cwd(), '..')

function runSql(sql: string): void {
  execFileSync(
    'docker',
    ['compose', 'exec', '-T', 'postgres', 'sh', '-c', 'psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"'],
    { cwd: repoRoot, input: sql },
  )
}

function assertLooksLikeEmail(email: string): void {
  if (!/^[\w.+-]+@[\w.-]+$/.test(email)) {
    throw new Error(`Refusing to interpolate unexpected email into SQL: ${email}`)
  }
}

/**
 * Grants the Admin role by writing to the dev Postgres container directly. There's deliberately
 * no API to create the first admin (config-driven bootstrap only), so e2e tests take the same
 * shortcut an operator would. The role row exists because the backend creates it on startup.
 */
export function promoteToAdmin(email: string): void {
  assertLooksLikeEmail(email)
  runSql(`INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
    SELECT u."Id", r."Id" FROM "AspNetUsers" u, "AspNetRoles" r
    WHERE u."Email" = '${email}' AND r."Name" = 'Admin'
    ON CONFLICT DO NOTHING;`)
}

/**
 * Demotes an account from Admin if it currently has the role (idempotent). An admin can't demote
 * themselves through the API by design (AdminEndpoints), so a test that promotes the shared
 * login-mode account (see adminHelper.ts's registerAdminViaApi) has no self-service way to undo
 * it — without this, that promotion would otherwise stick around for every test that follows, in
 * this run and every one after. registerViaApi calls this before every login-mode sign-in.
 */
export function resetAdminRole(email: string): void {
  assertLooksLikeEmail(email)
  runSql(`DELETE FROM "AspNetUserRoles"
    WHERE "UserId" = (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = '${email}')
    AND "RoleId" = (SELECT "Id" FROM "AspNetRoles" WHERE "Name" = 'Admin');`)
}

export function uniqueTestUser(displayName = 'Test Trainer'): TestUser {
  return {
    email: `e2e-${Date.now()}-${Math.random().toString(36).slice(2)}@example.com`,
    password: 'Password1',
    displayName,
  }
}

/**
 * Clears everything a previous test (this run or an earlier one) could have left on an
 * account: owned cards, trade-list entries, and an enabled share link. `registerViaApi` calls
 * this automatically whenever it signs into the shared login-mode account rather than
 * registering a fresh one, so every test starts from a known-empty state without needing its
 * own cleanup code. Only clearing *before* a test runs (not after) is enough: a test that fails
 * partway through just gets cleaned up by the next test's own call, same as this one's.
 */
export async function wipeAccountData(page: Page): Promise<void> {
  const collection: { cardId: string }[] = await (await page.request.get('/api/collection')).json()
  for (const entry of collection) {
    await page.request.delete(`/api/collection/${entry.cardId}`, { headers: csrfHeaders })
  }

  const tradeList: { cardId: string, direction: string }[] = await (await page.request.get('/api/trade-list')).json()
  for (const entry of tradeList) {
    await page.request.delete(`/api/trade-list/${entry.cardId}/${entry.direction}`, { headers: csrfHeaders })
  }

  const share: { enabled: boolean } = await (await page.request.get('/api/trade-list/share')).json()
  if (share.enabled) {
    await page.request.delete('/api/trade-list/share', { headers: csrfHeaders })
  }
}

/**
 * Registers a fresh user against the real backend API (not mocked) and leaves
 * the browser context's session cookie set, so the caller can navigate straight
 * into authenticated pages without re-doing the signup form in every test.
 */
export async function registerViaApi(page: Page, user: TestUser = uniqueTestUser()): Promise<TestUser> {
  // An admin can close registration on the dev backend. Set E2E_LOGIN_EMAIL / E2E_LOGIN_PASSWORD
  // to sign in as an existing account instead — only safe for tests that don't change its own
  // login credentials (see registerDisposableUser for those). Every test shares this one
  // account, so it's wiped clean before handing it back.
  const loginEmail = process.env.E2E_LOGIN_EMAIL
  if (loginEmail) {
    const login = await page.request.post('/api/auth/login', {
      headers: csrfHeaders,
      data: { email: loginEmail, password: process.env.E2E_LOGIN_PASSWORD ?? '' },
    })
    if (!login.ok()) {
      throw new Error(`Failed to log in as ${loginEmail}: ${login.status()}`)
    }
    const loginBody = await login.json()
    if (loginBody.requiresTwoFactor) {
      throw new Error(`${loginEmail} has two-factor authentication enabled — turn it off for the e2e login-mode account.`)
    }
    resetAdminRole(loginEmail)
    await wipeAccountData(page)
    return { email: loginEmail, password: process.env.E2E_LOGIN_PASSWORD ?? '', displayName: loginBody.user.displayName }
  }

  const response = await page.request.post('/api/auth/register', {
    headers: csrfHeaders,
    data: user,
  })
  if (!response.ok()) {
    throw new Error(`Failed to register test user: ${response.status()} ${await response.text()}`)
  }
  return user
}

/**
 * Registers a genuinely fresh, disposable account, ignoring E2E_LOGIN_EMAIL even when set —
 * for specs that change or destroy login credentials themselves (password, email, 2FA, or the
 * account), where reusing the shared login-mode account would break every other test that
 * depends on its credentials still working.
 */
export async function registerDisposableUser(page: Page): Promise<TestUser> {
  const user = uniqueTestUser()
  const response = await page.request.post('/api/auth/register', {
    headers: csrfHeaders,
    data: user,
  })
  if (!response.ok()) {
    throw new Error(`Failed to register a disposable test user: ${response.status()} ${await response.text()}. `
      + 'This spec needs registration open on the target backend, regardless of E2E_LOGIN_EMAIL.')
  }
  return user
}
