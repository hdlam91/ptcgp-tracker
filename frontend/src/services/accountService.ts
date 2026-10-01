import type { RecoveryCodesResponse, TwoFactorSetupResponse, TwoFactorStatusResponse } from '@/types/api'
import { httpClient } from '@/services/httpClient'

export const accountService = {
  changePassword: (currentPassword: string, newPassword: string) =>
    httpClient.post<void>('/account/password', { currentPassword, newPassword }),

  changeEmail: (newEmail: string, currentPassword: string) =>
    httpClient.post<void>('/account/email', { newEmail, currentPassword }),

  updateLocale: (locale: string) => httpClient.post<void>('/account/locale', { locale }),

  deleteAccount: (currentPassword: string) =>
    httpClient.delete<void>('/account', { currentPassword }),

  getTwoFactorStatus: () => httpClient.get<TwoFactorStatusResponse>('/account/2fa'),

  setupTwoFactor: () => httpClient.get<TwoFactorSetupResponse>('/account/2fa/setup'),

  enableTwoFactor: (code: string) =>
    httpClient.post<RecoveryCodesResponse>('/account/2fa/enable', { code }),

  disableTwoFactor: (currentPassword: string) =>
    httpClient.post<void>('/account/2fa/disable', { currentPassword }),

  regenerateRecoveryCodes: (currentPassword: string) =>
    httpClient.post<RecoveryCodesResponse>('/account/2fa/recovery-codes', { currentPassword }),
}
