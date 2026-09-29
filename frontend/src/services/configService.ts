import type { PublicConfigResponse } from '@/types/api'
import { httpClient } from '@/services/httpClient'

export const configService = {
  get: () => httpClient.get<PublicConfigResponse>('/config'),
}
