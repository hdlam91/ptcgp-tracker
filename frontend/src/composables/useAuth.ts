import { computed, ref } from 'vue'
import { ApiError } from '@/services/httpClient'
import { authService, type LoginPayload, type RegisterPayload } from '@/services/authService'
import type { UserResponse } from '@/types/api'

// Module-scoped singleton: every component that calls useAuth() shares the same
// reactive state, so there's one source of truth for "who's logged in" without
// pulling in a state-management library for this alone.
const currentUser = ref<UserResponse | null>(null)
const initialized = ref(false)

async function fetchCurrentUser(): Promise<UserResponse | null> {
  try {
    currentUser.value = await authService.me()
  }
  catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      currentUser.value = null
    }
    else {
      throw error
    }
  }
  finally {
    initialized.value = true
  }
  return currentUser.value
}

async function register(payload: RegisterPayload) {
  currentUser.value = await authService.register(payload)
  initialized.value = true
}

async function login(payload: LoginPayload) {
  currentUser.value = await authService.login(payload)
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
    initialized: computed(() => initialized.value),
    fetchCurrentUser,
    register,
    login,
    logout,
  }
}
