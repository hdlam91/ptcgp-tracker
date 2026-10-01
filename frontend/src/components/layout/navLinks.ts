export interface NavLink {
  to: string
  /** Key under layout.nav.* for the full label; `${key}Short` holds the short label, when present. */
  key: string
  /** Whether a shorter label exists for the inline menu on phones, where space is tight. */
  hasShortLabel?: boolean
}

export const navLinks: NavLink[] = [
  { to: '/', key: 'collection' },
  { to: '/cards', key: 'allCards', hasShortLabel: true },
  { to: '/trade-list', key: 'tradeList', hasShortLabel: true },
]
