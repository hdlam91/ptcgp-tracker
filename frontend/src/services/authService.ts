import type { LoginResponse, UserResponse } from '@/types/api'
import { httpClient } from '@/services/httpClient'

export interface RegisterPayload {
  email: string
  password: string
  displayName: string
}

export interface LoginPayload {
  email: string
  password: string
}

export interface TwoFactorLoginPayload {
  code: string
  isRecoveryCode: boolean
  rememberDevice: boolean
}

export interface ResetPasswordPayload {
  email: string
  token: string
  newPassword: string
}

export const authService = {
  register: (payload: RegisterPayload) => httpClient.post<UserResponse>('/auth/register', payload),
  login: (payload: LoginPayload) => httpClient.post<LoginResponse>('/auth/login', payload),
  loginTwoFactor: (payload: TwoFactorLoginPayload) => httpClient.post<UserResponse>('/auth/login/2fa', payload),
  logout: () => httpClient.post<void>('/auth/logout'),
  me: () => httpClient.get<UserResponse>('/auth/me'),
  forgotPassword: (email: string) => httpClient.post<void>('/auth/forgot-password', { email }),
  resetPassword: (payload: ResetPasswordPayload) => httpClient.post<void>('/auth/reset-password', payload),
}
