import { httpClient } from '@/services/httpClient'
import type { SharedTradeListResponse, TradeDirection, TradeListEntryResponse, TradeListShareStatusResponse } from '@/types/api'

export const tradeListService = {
  list: (direction?: TradeDirection) =>
    httpClient.get<TradeListEntryResponse[]>(direction ? `/trade-list?direction=${direction}` : '/trade-list'),
  add: (cardId: string, direction: TradeDirection) =>
    httpClient.post<TradeListEntryResponse>('/trade-list', { cardId, direction }),
  remove: (cardId: string, direction: TradeDirection) =>
    httpClient.delete<void>(`/trade-list/${encodeURIComponent(cardId)}/${direction}`),
  getShareStatus: () => httpClient.get<TradeListShareStatusResponse>('/trade-list/share'),
  enableSharing: () => httpClient.post<TradeListShareStatusResponse>('/trade-list/share'),
  disableSharing: () => httpClient.delete<void>('/trade-list/share'),
}

/** Public, unauthenticated lookup for a shared trade-list link — no cookie needed. */
export function getSharedTradeList(token: string) {
  return httpClient.get<SharedTradeListResponse>(`/trade-list/shared/${encodeURIComponent(token)}`)
}
