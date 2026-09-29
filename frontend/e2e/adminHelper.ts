import type { APIRequestContext, Page } from '@playwright/test'
import { expect } from '@playwright/test'
import { promoteToAdmin, registerViaApi, uniqueTestUser, type TestUser } from './testUsers'

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
