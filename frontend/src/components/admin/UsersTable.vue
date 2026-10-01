<script setup lang="ts">
import type { AdminUserResponse } from '@/types/api'
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Button } from '@/components/ui/button'

defineProps<{
  users: AdminUserResponse[]
  currentUserId: string | undefined
  busy: boolean
}>()

defineEmits<{
  (e: 'setAdmin', user: AdminUserResponse, isAdmin: boolean): void
  (e: 'disableShare', user: AdminUserResponse): void
  (e: 'resendConfirmation', user: AdminUserResponse): void
  (e: 'delete', user: AdminUserResponse): void
}>()

const { t } = useI18n()

// Deleting is destructive and permanent, so it takes a second, explicit click.
const confirmingDeleteId = ref<string | null>(null)
</script>

<template>
  <div class="overflow-x-auto">
    <table class="w-full min-w-[46rem] text-left text-sm">
      <thead class="text-xs uppercase tracking-wide text-muted-foreground">
        <tr class="border-b">
          <th class="py-2 pr-4 font-medium">
            {{ t('admin.users.columnUser') }}
          </th>
          <th class="py-2 pr-4 font-medium">
            {{ t('admin.users.columnJoined') }}
          </th>
          <th class="py-2 pr-4 text-right font-medium">
            {{ t('admin.users.columnCards') }}
          </th>
          <th class="py-2 pr-4 text-right font-medium">
            {{ t('admin.users.columnWant') }}
          </th>
          <th class="py-2 pr-4 text-right font-medium">
            {{ t('admin.users.columnOffer') }}
          </th>
          <th class="py-2 pr-4 font-medium">
            {{ t('admin.users.columnShareLink') }}
          </th>
          <th class="py-2 text-right font-medium">
            {{ t('admin.users.columnActions') }}
          </th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="user in users" :key="user.id" class="border-b last:border-0" :data-testid="`user-row-${user.email}`">
          <td class="py-3 pr-4">
            <div class="flex flex-wrap items-center gap-2">
              <span class="font-medium">{{ user.displayName }}</span>
              <span
                v-if="user.isAdmin"
                class="rounded-full border border-sky-300 bg-sky-100 px-2 py-0.5 text-xs font-medium text-sky-700 dark:border-sky-800 dark:bg-sky-950 dark:text-sky-300"
              >{{ t('admin.users.adminBadge') }}</span>
              <span
                v-if="!user.emailConfirmed"
                class="rounded-full border border-amber-300 bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-700 dark:border-amber-800 dark:bg-amber-950 dark:text-amber-300"
              >{{ t('admin.users.unconfirmedBadge') }}</span>
              <span v-if="user.id === currentUserId" class="text-xs text-muted-foreground">{{ t('admin.users.you') }}</span>
            </div>
            <div class="text-xs text-muted-foreground">
              {{ user.email }}
            </div>
          </td>
          <td class="whitespace-nowrap py-3 pr-4 tabular-nums">
            {{ new Date(user.createdAt).toLocaleDateString() }}
          </td>
          <td class="py-3 pr-4 text-right tabular-nums">
            {{ user.ownedUniqueCards }}
          </td>
          <td class="py-3 pr-4 text-right tabular-nums">
            {{ user.wantCount }}
          </td>
          <td class="py-3 pr-4 text-right tabular-nums">
            {{ user.offerCount }}
          </td>
          <td class="py-3 pr-4">
            <div v-if="user.shareHandle" class="flex items-center gap-2">
              <a :href="`/share/${user.shareHandle}`" target="_blank" rel="noopener" class="text-primary underline-offset-4 hover:underline">/share/{{ user.shareHandle }}</a>
              <Button variant="ghost" size="sm" :disabled="busy" @click="$emit('disableShare', user)">
                {{ t('admin.users.disableShare') }}
              </Button>
            </div>
            <span v-else class="text-muted-foreground">{{ t('admin.users.notApplicable') }}</span>
          </td>
          <td class="py-3 text-right">
            <span v-if="user.id === currentUserId" class="text-xs text-muted-foreground">{{ t('admin.users.notApplicable') }}</span>
            <div v-else-if="confirmingDeleteId === user.id" class="flex items-center justify-end gap-2">
              <span class="text-xs text-muted-foreground">{{ t('admin.users.confirmDeleteText', { name: user.displayName }) }}</span>
              <Button variant="destructive" size="sm" :disabled="busy" @click="$emit('delete', user); confirmingDeleteId = null">
                {{ t('admin.users.confirmDelete') }}
              </Button>
              <Button variant="outline" size="sm" @click="confirmingDeleteId = null">
                {{ t('admin.users.cancel') }}
              </Button>
            </div>
            <div v-else class="flex items-center justify-end gap-2">
              <Button v-if="!user.emailConfirmed" variant="outline" size="sm" :disabled="busy" @click="$emit('resendConfirmation', user)">
                {{ t('admin.users.resend') }}
              </Button>
              <Button variant="outline" size="sm" :disabled="busy" @click="$emit('setAdmin', user, !user.isAdmin)">
                {{ user.isAdmin ? t('admin.users.removeAdmin') : t('admin.users.makeAdmin') }}
              </Button>
              <Button variant="outline" size="sm" :disabled="busy" @click="confirmingDeleteId = user.id">
                {{ t('admin.users.delete') }}
              </Button>
            </div>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
