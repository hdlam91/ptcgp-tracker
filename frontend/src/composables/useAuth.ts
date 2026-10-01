import type { ConfirmEmailPayload, LoginPayload, RegisterPayload, TwoFactorLoginPayload } from '@/services/authService'
import type { UserResponse } from '@/types/api'
import { computed, ref } from 'vue'
import { authService } from '@/services/authService'
import { ApiError } from '@/services/httpClient'

// Module-scoped singleton: every component that calls useAuth() shares the same
// reactive state, so there's one source of truth for "who's logged in" without
// pulling in a state-management library for this alone.
const currentUser = ref<UserResponse | null>(null)
const initialized = ref(false)
// True when the startup check couldn't reach the server (offline, or the server is down), as
// opposed to the server answering "not logged in".
const connectionError = ref(false)

async function fetchCurrentUser(): Promise<UserResponse | null> {
  try {
    currentUser.value = await authService.me()
    connectionError.value = false
    initialized.value = true
  }
  catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      currentUser.value = null
      connectionError.value = false
      initialized.value = true
    }
    else if (error instanceof ApiError && error.status < 500) {
      throw error
    }
    else {
      // A network failure (fetch throws) or a 5xx. Stay uninitialized so the next navigation retries.
      connectionError.value = true
    }
  }
  return currentUser.value
}

/**
 * Returns the raw response so the register view can branch on `requiresEmailConfirmation` —
 * `currentUser` is only set once registration actually signs someone in.
 */
async function register(payload: RegisterPayload) {
  const response = await authService.register(payload)
  if (response.user) {
    currentUser.value = response.user
    initialized.value = true
  }
  return response
}

/**
 * Returns the raw response so the login view can branch on `requiresTwoFactor` — `currentUser`
 * is only set once a login (with or without a 2FA step) actually completes.
 */
async function login(payload: LoginPayload) {
  const response = await authService.login(payload)
  if (response.user) {
    currentUser.value = response.user
    initialized.value = true
  }
  return response
}

async function loginTwoFactor(payload: TwoFactorLoginPayload) {
  currentUser.value = await authService.loginTwoFactor(payload)
  initialized.value = true
}

async function confirmEmail(payload: ConfirmEmailPayload) {
  currentUser.value = await authService.confirmEmail(payload)
  initialized.value = true
}

async function logout() {
  await authService.logout()
  currentUser.value = null
}

export function useAuth() {
  return {
    currentUser: computed(() => currentUser.value),
    isAuthenticated: computed(() => currentUser.value !== null),
    isAdmin: computed(() => currentUser.value?.isAdmin === true),
    initialized: computed(() => initialized.value),
    connectionError: computed(() => connectionError.value),
    fetchCurrentUser,
    register,
    login,
    loginTwoFactor,
    confirmEmail,
    logout,
  }
}
