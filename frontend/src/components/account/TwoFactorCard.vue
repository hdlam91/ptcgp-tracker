<script setup lang="ts">
import { nextTick, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAccount } from '@/composables/useAccount'

const { t } = useI18n()
const { busy, error, getTwoFactorStatus, setupTwoFactor, enableTwoFactor, disableTwoFactor, regenerateRecoveryCodes } = useAccount()

const enabled = ref<boolean | null>(null)

// The setup step: QR + manual key while waiting for a code to confirm it.
const settingUp = ref(false)
const sharedKey = ref('')
const code = ref('')
const qrCanvas = ref<HTMLCanvasElement | null>(null)

// Recovery codes are only ever shown right after they're (re)generated.
const recoveryCodes = ref<string[] | null>(null)

// Disabling and regenerating both need the current password first.
const disabling = ref(false)
const regenerating = ref(false)
const currentPassword = ref('')

onMounted(async () => {
  const status = await getTwoFactorStatus()
  if (status.ok)
    enabled.value = status.data.enabled
})

async function startSetup() {
  settingUp.value = true
  const setup = await setupTwoFactor()
  if (!setup.ok)
    return
  sharedKey.value = setup.data.sharedKey
  await nextTick()
  const qrcode = await import('qrcode')
  if (qrCanvas.value)
    await qrcode.toCanvas(qrCanvas.value, setup.data.otpAuthUri, { width: 200 })
}

function cancelSetup() {
  settingUp.value = false
  code.value = ''
}

async function confirmEnable() {
  const result = await enableTwoFactor(code.value)
  if (result.ok) {
    enabled.value = true
    settingUp.value = false
    code.value = ''
    recoveryCodes.value = result.data.recoveryCodes
  }
}

async function confirmDisable() {
  const result = await disableTwoFactor(currentPassword.value)
  if (result.ok) {
    enabled.value = false
    disabling.value = false
    currentPassword.value = ''
    recoveryCodes.value = null
  }
}

async function confirmRegenerate() {
  const result = await regenerateRecoveryCodes(currentPassword.value)
  if (result.ok) {
    regenerating.value = false
    currentPassword.value = ''
    recoveryCodes.value = result.data.recoveryCodes
  }
}
</script>

<template>
  <Card>
    <CardHeader>
      <CardTitle class="text-base">
        {{ t('account.twoFactor.title') }}
      </CardTitle>
      <CardDescription>
        <template v-if="enabled === null">
          {{ t('account.twoFactor.loading') }}
        </template>
        <template v-else-if="enabled">
          {{ t('account.twoFactor.enabledDescription') }}
        </template>
        <template v-else>
          {{ t('account.twoFactor.disabledDescription') }}
        </template>
      </CardDescription>
    </CardHeader>
    <CardContent class="flex flex-col gap-4">
      <!-- Recovery codes: shown once, right after (re)generating them. -->
      <div v-if="recoveryCodes" class="flex flex-col gap-2 rounded-md border border-amber-300 bg-amber-50 p-3 text-sm dark:border-amber-800 dark:bg-amber-950">
        <p class="font-medium">
          {{ t('account.twoFactor.saveRecoveryCodesTitle') }}
        </p>
        <p class="text-muted-foreground">
          {{ t('account.twoFactor.saveRecoveryCodesDescription') }}
        </p>
        <ul class="grid grid-cols-2 gap-1 rounded bg-background p-2 font-mono text-sm">
          <li v-for="recoveryCode in recoveryCodes" :key="recoveryCode">
            {{ recoveryCode }}
          </li>
        </ul>
        <Button size="sm" class="self-start" @click="recoveryCodes = null">
          {{ t('account.twoFactor.savedRecoveryCodes') }}
        </Button>
      </div>

      <template v-else-if="enabled === false && !settingUp">
        <Button class="self-start" @click="startSetup">
          {{ t('account.twoFactor.enable') }}
        </Button>
      </template>

      <form v-else-if="settingUp" class="flex flex-col gap-3" @submit.prevent="confirmEnable">
        <p class="text-sm text-muted-foreground">
          {{ t('account.twoFactor.setupInstructions') }}
        </p>
        <canvas ref="qrCanvas" class="rounded-md border" />
        <p class="break-all rounded-md border bg-muted px-2 py-1.5 font-mono text-sm">
          {{ sharedKey }}
        </p>
        <div class="flex flex-col gap-1.5">
          <Label for="totp-code">{{ t('account.twoFactor.code') }}</Label>
          <Input id="totp-code" v-model="code" inputmode="numeric" autocomplete="one-time-code" required />
        </div>
        <p v-if="error" class="text-sm text-destructive">
          {{ error }}
        </p>
        <div class="flex items-center gap-2">
          <Button type="submit" :disabled="busy">
            {{ busy ? t('account.twoFactor.enabling') : t('account.twoFactor.confirmEnable') }}
          </Button>
          <Button type="button" variant="outline" :disabled="busy" @click="cancelSetup">
            {{ t('account.twoFactor.cancel') }}
          </Button>
        </div>
      </form>

      <template v-else-if="enabled">
        <div v-if="!disabling && !regenerating" class="flex flex-wrap items-center gap-2">
          <Button variant="outline" @click="regenerating = true">
            {{ t('account.twoFactor.regenerateRecoveryCodes') }}
          </Button>
          <Button variant="destructive" @click="disabling = true">
            {{ t('account.twoFactor.disable') }}
          </Button>
        </div>
        <form v-else class="flex flex-col gap-3" @submit.prevent="disabling ? confirmDisable() : confirmRegenerate()">
          <div class="flex flex-col gap-1.5">
            <Label for="twofactor-current-password">{{ t('account.twoFactor.currentPassword') }}</Label>
            <Input id="twofactor-current-password" v-model="currentPassword" type="password" autocomplete="current-password" required />
          </div>
          <p v-if="error" class="text-sm text-destructive">
            {{ error }}
          </p>
          <div class="flex items-center gap-2">
            <Button type="submit" :variant="disabling ? 'destructive' : 'default'" :disabled="busy">
              {{ busy ? t('account.twoFactor.confirming') : disabling ? t('account.twoFactor.confirmDisable') : t('account.twoFactor.confirmRegenerate') }}
            </Button>
            <Button type="button" variant="outline" :disabled="busy" @click="disabling = false; regenerating = false; currentPassword = ''">
              {{ t('account.twoFactor.cancel') }}
            </Button>
          </div>
        </form>
      </template>
    </CardContent>
  </Card>
</template>
