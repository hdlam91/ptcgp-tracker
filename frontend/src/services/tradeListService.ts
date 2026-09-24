import { httpClient } from '@/services/httpClient'
import type { TradeDirection, TradeListEntryResponse } from '@/types/api'

export const tradeListService = {
  list: (direction?: TradeDirection) =>
    httpClient.get<TradeListEntryResponse[]>(direction ? `/trade-list?direction=${direction}` : '/trade-list'),
  add: (cardId: string, direction: TradeDirection) =>
    httpClient.post<TradeListEntryResponse>('/trade-list', { cardId, direction }),
  remove: (cardId: string, direction: TradeDirection) =>
    httpClient.delete<void>(`/trade-list/${encodeURIComponent(cardId)}/${direction}`),
}
