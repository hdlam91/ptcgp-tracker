import { ref } from 'vue'
import { i18n } from '@/i18n'
import { accountService } from '@/services/accountService'
import { ApiError } from '@/services/httpClient'

function describe(error: unknown): string {
  if (error instanceof ApiError) {
    const body = error.body as { errors?: Record<string, string[]>, error?: string, detail?: string } | null
    const firstValidationError = body?.errors ? Object.values(body.errors)[0]?.[0] : undefined
    return firstValidationError ?? body?.error ?? body?.detail ?? i18n.global.t('errors.requestFailed', { status: error.status })
  }
  return i18n.global.t('errors.generic')
}

/**
 * Not a module-scoped singleton (unlike useAuth/useCollection): each card on the account page
 * calls this independently, so one form's busy/error state never bleeds into another's.
 */
export function useAccount() {
  const busy = ref(false)
  const error = ref('')

  // A plain `T | undefined` return can't tell "succeeded with no body" (void endpoints like
  // change-password return 204) apart from "failed" — both are undefined. A tagged result fixes
  // that ambiguity for every action, not just the ones that happen to return data today.
  async function run<T>(action: () => Promise<T>): Promise<{ ok: true, data: T } | { ok: false }> {
    error.value = ''
    busy.value = true
    try {
      return { ok: true, data: await action() }
    }
    catch (e) {
      error.value = describe(e)
      return { ok: false }
    }
    finally {
      busy.value = false
    }
  }

  return {
    busy,
    error,
    changePassword: (currentPassword: string, newPassword: string) =>
      run(() => accountService.changePassword(currentPassword, newPassword)),
    changeEmail: (newEmail: string, currentPassword: string) =>
      run(() => accountService.changeEmail(newEmail, currentPassword)),
    deleteAccount: (currentPassword: string) =>
      run(() => accountService.deleteAccount(currentPassword)),
    getTwoFactorStatus: () => run(() => accountService.getTwoFactorStatus()),
    setupTwoFactor: () => run(() => accountService.setupTwoFactor()),
    enableTwoFactor: (code: string) => run(() => accountService.enableTwoFactor(code)),
    disableTwoFactor: (currentPassword: string) => run(() => accountService.disableTwoFactor(currentPassword)),
    regenerateRecoveryCodes: (currentPassword: string) => run(() => accountService.regenerateRecoveryCodes(currentPassword)),
  }
}
