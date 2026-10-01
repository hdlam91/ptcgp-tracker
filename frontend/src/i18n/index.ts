import { createI18n } from 'vue-i18n'
import en from '@/i18n/locales/en.json'

// Composition API mode throughout (useI18n() in <script setup>), matching the rest of the app's
// Composition-only convention. Only English exists today; useLocale.ts is where a user's stored
// preference (or, for logged-out visitors, the browser's) gets applied to `locale` at runtime.
export const i18n = createI18n({
  legacy: false,
  locale: 'en',
  fallbackLocale: 'en',
  messages: { en },
})
