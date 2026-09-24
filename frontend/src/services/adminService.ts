import { httpClient } from '@/services/httpClient'
import type { AdminSettingsResponse, AdminUserResponse, CatalogStatusResponse } from '@/types/api'

export const adminService = {
  users: () => httpClient.get<AdminUserResponse[]>('/admin/users'),
  setAdmin: (userId: string, isAdmin: boolean) =>
    httpClient.put<void>(`/admin/users/${encodeURIComponent(userId)}/admin`, { isAdmin }),
  deleteUser: (userId: string) => httpClient.delete<void>(`/admin/users/${encodeURIComponent(userId)}`),
  disableShare: (userId: string) => httpClient.delete<void>(`/admin/users/${encodeURIComponent(userId)}/share`),
  getSettings: () => httpClient.get<AdminSettingsResponse>('/admin/settings'),
  updateSettings: (registrationOpen: boolean) =>
    httpClient.put<AdminSettingsResponse>('/admin/settings', { registrationOpen }),
  catalog: () => httpClient.get<CatalogStatusResponse>('/admin/catalog'),
  refreshCatalog: () => httpClient.post<CatalogStatusResponse>('/admin/catalog/refresh'),
}
