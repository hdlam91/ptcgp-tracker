import { httpClient } from '@/services/httpClient'
import type { UserResponse } from '@/types/api'

export interface RegisterPayload {
  email: string
  password: string
  displayName: string
}

export interface LoginPayload {
  email: string
  password: string
}

export const authService = {
  register: (payload: RegisterPayload) => httpClient.post<UserResponse>('/auth/register', payload),
  login: (payload: LoginPayload) => httpClient.post<UserResponse>('/auth/login', payload),
  logout: () => httpClient.post<void>('/auth/logout'),
  me: () => httpClient.get<UserResponse>('/auth/me'),
}
