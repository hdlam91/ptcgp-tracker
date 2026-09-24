import { execFileSync } from 'node:child_process'
import path from 'node:path'
import type { APIRequestContext, Page } from '@playwright/test'
import { expect } from '@playwright/test'
import { registerViaApi, uniqueTestUser, type TestUser } from './testUsers'

// Playwright runs from frontend/; docker-compose.yml lives one level up.
const repoRoot = path.resolve(process.cwd(), '..')

/**
 * Grants the Admin role by writing to the dev Postgres container directly. There's
 * deliberately no API to create the first admin (config-driven bootstrap only), so
 * e2e tests take the same shortcut an operator would. The role row exists because
 * the backend creates it on startup.
 */
function promoteToAdmin(email: string) {
  if (!/^[\w.+-]+@[\w.-]+$/.test(email)) {
    throw new Error(`Refusing to interpolate unexpected email into SQL: ${email}`)
  }
  const sql = `INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
    SELECT u."Id", r."Id" FROM "AspNetUsers" u, "AspNetRoles" r
    WHERE u."Email" = '${email}' AND r."Name" = 'Admin'
    ON CONFLICT DO NOTHING;`
  execFileSync(
    'docker',
    ['compose', 'exec', '-T', 'postgres', 'sh', '-c', 'psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"'],
    { cwd: repoRoot, input: sql },
  )
}

/** Registers a user on `page` and makes them an admin. Their session picks the role up on the next request. */
export async function registerAdminViaApi(page: Page): Promise<TestUser> {
  const user = await registerViaApi(page, uniqueTestUser('Admin Trainer'))
  promoteToAdmin(user.email)
  return user
}

/** Registers a separate user with its own cookie jar, so the page's admin session is untouched. */
export async function registerOtherUser(request: APIRequestContext, displayName = 'Other Trainer'): Promise<TestUser> {
  const user = uniqueTestUser(displayName)
  const response = await request.post('/api/auth/register', {
    headers: { 'X-Requested-With': 'XMLHttpRequest' },
    data: user,
  })
  expect(response.ok()).toBe(true)
  return user
}
