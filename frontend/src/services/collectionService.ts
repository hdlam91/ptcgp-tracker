import type { CollectionEntryResponse, SetSummaryResponse } from '@/types/api'
import { httpClient } from '@/services/httpClient'

export const collectionService = {
  list: (setCode?: string) =>
    httpClient.get<CollectionEntryResponse[]>(setCode ? `/collection?setCode=${encodeURIComponent(setCode)}` : '/collection'),
  summary: () => httpClient.get<SetSummaryResponse[]>('/collection/summary'),
  // Returns one entry per reprint that's the same real-world card as cardId (see the backend's
  // CardEquivalenceGrouping) — usually just cardId itself, but never fewer than that.
  setOwnedCount: (cardId: string, ownedCount: number) =>
    httpClient.put<CollectionEntryResponse[]>(`/collection/${encodeURIComponent(cardId)}`, { ownedCount }),
}
