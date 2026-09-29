export interface UserResponse {
  id: string
  email: string
  displayName: string
  isAdmin: boolean
}

/**
 * `user` is null exactly when `requiresTwoFactor` is true — the password was right, but the
 * login isn't complete until the 2FA step succeeds.
 */
export interface LoginResponse {
  requiresTwoFactor: boolean
  user: UserResponse | null
}

export interface TwoFactorStatusResponse {
  enabled: boolean
}

export interface TwoFactorSetupResponse {
  sharedKey: string
  otpAuthUri: string
}

export interface RecoveryCodesResponse {
  recoveryCodes: string[]
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

export type ImageMirrorState = 'Idle' | 'Running' | 'Completed' | 'Failed'

export interface ImageMirrorStatusResponse {
  state: ImageMirrorState
  total: number
  completed: number
  failed: number
  startedAt: string | null
  finishedAt: string | null
  storedCards: number
  storedPacks: number
  storedBytes: number
  error: string | null
}
