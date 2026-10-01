<script setup lang="ts">
import { Moon, Sun } from '@lucide/vue'
import { useI18n } from 'vue-i18n'
import { Button } from '@/components/ui/button'
import { useTheme } from '@/composables/useTheme'

const { t } = useI18n()
const { isDark, toggleTheme } = useTheme()
</script>

<template>
  <Button
    variant="outline"
    size="icon"
    :title="isDark ? t('layout.themeToggle.switchToLightMode') : t('layout.themeToggle.switchToDarkMode')"
    @click="toggleTheme"
  >
    <Sun v-if="isDark" class="size-4" />
    <Moon v-else class="size-4" />
  </Button>
</template>
