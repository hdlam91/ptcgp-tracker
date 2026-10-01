<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { LOCALES, useLocale } from '@/composables/useLocale'
import { ApiError } from '@/services/httpClient'

const { t } = useI18n()
const { locale, setLocale } = useLocale()
const busy = ref(false)
const error = ref('')

async function onChange(event: Event) {
  const code = (event.target as HTMLSelectElement).value as typeof LOCALES[number]['code']
  error.value = ''
  busy.value = true
  try {
    await setLocale(code)
  }
  catch (e) {
    error.value = e instanceof ApiError ? t('errors.requestFailed', { status: e.status }) : t('errors.generic')
  }
  finally {
    busy.value = false
  }
}

const selectClass = 'h-10 rounded-md border border-input bg-background px-3 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2'
</script>

<template>
  <Card>
    <CardHeader>
      <CardTitle class="text-base">
        {{ t('account.language.title') }}
      </CardTitle>
      <CardDescription>{{ t('account.language.description') }}</CardDescription>
    </CardHeader>
    <CardContent class="flex flex-col gap-2">
      <select :value="locale" :disabled="busy" :class="selectClass" :aria-label="t('account.language.selectLabel')" @change="onChange">
        <option v-for="option in LOCALES" :key="option.code" :value="option.code">
          {{ option.label }}
        </option>
      </select>
      <p v-if="error" class="text-sm text-destructive">
        {{ error }}
      </p>
    </CardContent>
  </Card>
</template>
