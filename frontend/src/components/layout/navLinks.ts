export interface NavLink {
  to: string
  label: string
  /** Shorter text for the inline menu on phones, where space is tight. */
  shortLabel?: string
}

export const navLinks: NavLink[] = [
  { to: '/', label: 'Collection' },
  { to: '/cards', label: 'All cards', shortLabel: 'Cards' },
  { to: '/trade-list', label: 'Trade list', shortLabel: 'Trades' },
]
