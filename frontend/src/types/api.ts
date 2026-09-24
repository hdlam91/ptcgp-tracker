export interface UserResponse {
  id: string
  email: string
  displayName: string
  isAdmin: boolean
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
  handle: string | null
}

export interface SharedTradeListResponse {
  displayName: string
  entries: TradeListEntryResponse[]
}

export interface AdminUserResponse {
  id: string
  email: string
  displayName: string
  createdAt: string
  isAdmin: boolean
  ownedUniqueCards: number
  wantCount: number
  offerCount: number
  shareHandle: string | null
}

export interface AdminSettingsResponse {
  registrationOpen: boolean
}

export interface CatalogStatusResponse {
  repoTag: string
  cardCount: number
  lastRefreshedAt: string | null
  lastError: string | null
}

export interface PublicConfigResponse {
  registrationOpen: boolean
}
