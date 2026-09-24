import { httpClient } from '@/services/httpClient'
import type { CollectionEntryResponse, SetSummaryResponse } from '@/types/api'

export const collectionService = {
  list: (setCode?: string) =>
    httpClient.get<CollectionEntryResponse[]>(setCode ? `/collection?setCode=${encodeURIComponent(setCode)}` : '/collection'),
  summary: () => httpClient.get<SetSummaryResponse[]>('/collection/summary'),
  setOwnedCount: (cardId: string, ownedCount: number) =>
    httpClient.put<CollectionEntryResponse>(`/collection/${encodeURIComponent(cardId)}`, { ownedCount }),
}
