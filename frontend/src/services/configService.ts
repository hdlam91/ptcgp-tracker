import { httpClient } from '@/services/httpClient'
import type { PublicConfigResponse } from '@/types/api'

export const configService = {
  get: () => httpClient.get<PublicConfigResponse>('/config'),
}
