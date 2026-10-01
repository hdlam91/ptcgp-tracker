<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const { t } = useI18n()
</script>

<template>
  <div class="mx-auto flex max-w-2xl flex-col gap-6 px-4 py-10">
    <div>
      <RouterLink to="/" class="text-sm text-muted-foreground hover:text-foreground">
        {{ t('legal.backLink') }}
      </RouterLink>
      <h1 class="mt-2 text-2xl font-semibold">
        {{ t('legal.pageTitle') }}
      </h1>
    </div>

    <Card>
      <CardHeader>
        <CardTitle class="text-base">
          {{ t('legal.notAffiliated.title') }}
        </CardTitle>
      </CardHeader>
      <CardContent class="flex flex-col gap-3 text-sm text-muted-foreground">
        <p>
          {{ t('legal.notAffiliated.descriptionBeforeEm') }}
          <em>{{ t('legal.notAffiliated.descriptionEm') }}</em>{{ t('legal.notAffiliated.descriptionAfterEm') }}
        </p>
        <p>
          {{ t('legal.notAffiliated.trademarkNotice') }}
        </p>
      </CardContent>
    </Card>

    <Card>
      <CardHeader>
        <CardTitle class="text-base">
          {{ t('legal.cardData.title') }}
        </CardTitle>
      </CardHeader>
      <CardContent class="flex flex-col gap-3 text-sm text-muted-foreground">
        <p>
          {{ t('legal.cardData.beforeLink') }}
          <a
            href="https://github.com/PocketDecks/pokemon-tcg-pocket-cards"
            target="_blank"
            rel="noopener noreferrer"
            class="font-medium text-primary underline-offset-4 hover:underline"
          >{{ t('legal.cardData.linkText') }}</a>
          {{ t('legal.cardData.afterLink') }}
        </p>
      </CardContent>
    </Card>

    <Card>
      <CardHeader>
        <CardTitle class="text-base">
          {{ t('legal.noWarranty.title') }}
        </CardTitle>
      </CardHeader>
      <CardContent class="text-sm text-muted-foreground">
        <p>
          {{ t('legal.noWarranty.description') }}
        </p>
      </CardContent>
    </Card>

    <p class="text-xs text-muted-foreground">
      {{ t('legal.footer.contactNotice') }}
    </p>
  </div>
</template>
