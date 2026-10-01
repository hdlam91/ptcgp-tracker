import { computed } from 'vue'
import { i18n } from '@/i18n'
import { accountService } from '@/services/accountService'

/**
 * Keep in sync with the backend's SupportedLocales (backend/.../Services/SupportedLocales.cs) —
 * adding a language means adding it here, plus a new frontend/src/i18n/locales/<code>.json.
 */
export const LOCALES = [
  { code: 'en', label: 'English' },
] as const

export type LocaleCode = (typeof LOCALES)[number]['code']

/**
 * Module-scoped singleton, same pattern as useAuth: one reactive `locale` shared everywhere.
 * `applyFromUser` is called by useAuth whenever a user becomes known (login, register,
 * /me) — logged-out pages just keep whatever createI18n was given (`'en'`), since there's no
 * account yet to read a preference from.
 */
export function useLocale() {
  function applyFromUser(preferredLocale: string) {
    if (LOCALES.some(l => l.code === preferredLocale)) {
      i18n.global.locale.value = preferredLocale as LocaleCode
    }
  }

  async function setLocale(code: LocaleCode) {
    i18n.global.locale.value = code
    await accountService.updateLocale(code)
  }

  return {
    locale: computed(() => i18n.global.locale.value),
    applyFromUser,
    setLocale,
  }
}
