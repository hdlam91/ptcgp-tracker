export interface UserResponse {
  id: string
  email: string
  displayName: string
}

export type TradeDirection = 'Want' | 'Offer'

export interface CollectionEntryResponse {
  cardId: string
  ownedCount: number
  updatedAt: string
}

export interface SetSummaryResponse {
  setCode: string
  ownedUniqueCards: number
  ownedCopiesTotal: number
}

export interface TradeListEntryResponse {
  cardId: string
  direction: TradeDirection
  createdAt: string
}

export interface TradeListShareStatusResponse {
  enabled: boolean
  token: string | null
}

export interface SharedTradeListResponse {
  displayName: string
  entries: TradeListEntryResponse[]
}
