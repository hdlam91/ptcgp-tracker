<script setup lang="ts">
import { ref } from 'vue'
import { Button } from '@/components/ui/button'
import type { AdminUserResponse } from '@/types/api'

defineProps<{
  users: AdminUserResponse[]
  currentUserId: string | undefined
  busy: boolean
}>()

defineEmits<{
  (e: 'set-admin', user: AdminUserResponse, isAdmin: boolean): void
  (e: 'disable-share', user: AdminUserResponse): void
  (e: 'delete', user: AdminUserResponse): void
}>()

// Deleting is destructive and permanent, so it takes a second, explicit click.
const confirmingDeleteId = ref<string | null>(null)
</script>

<template>
  <div class="overflow-x-auto">
    <table class="w-full min-w-[46rem] text-left text-sm">
      <thead class="text-xs uppercase tracking-wide text-muted-foreground">
        <tr class="border-b">
          <th class="py-2 pr-4 font-medium">
            User
          </th>
          <th class="py-2 pr-4 font-medium">
            Joined
          </th>
          <th class="py-2 pr-4 text-right font-medium">
            Cards
          </th>
          <th class="py-2 pr-4 text-right font-medium">
            Want
          </th>
          <th class="py-2 pr-4 text-right font-medium">
            Offer
          </th>
          <th class="py-2 pr-4 font-medium">
            Share link
          </th>
          <th class="py-2 text-right font-medium">
            Actions
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
              >Admin</span>
              <span v-if="user.id === currentUserId" class="text-xs text-muted-foreground">(you)</span>
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
              <Button variant="ghost" size="sm" :disabled="busy" @click="$emit('disable-share', user)">
                Disable
              </Button>
            </div>
            <span v-else class="text-muted-foreground">—</span>
          </td>
          <td class="py-3 text-right">
            <span v-if="user.id === currentUserId" class="text-xs text-muted-foreground">—</span>
            <div v-else-if="confirmingDeleteId === user.id" class="flex items-center justify-end gap-2">
              <span class="text-xs text-muted-foreground">Delete {{ user.displayName }} and all their data?</span>
              <Button variant="destructive" size="sm" :disabled="busy" @click="$emit('delete', user); confirmingDeleteId = null">
                Yes, delete
              </Button>
              <Button variant="outline" size="sm" @click="confirmingDeleteId = null">
                Cancel
              </Button>
            </div>
            <div v-else class="flex items-center justify-end gap-2">
              <Button variant="outline" size="sm" :disabled="busy" @click="$emit('set-admin', user, !user.isAdmin)">
                {{ user.isAdmin ? 'Remove admin' : 'Make admin' }}
              </Button>
              <Button variant="outline" size="sm" :disabled="busy" @click="confirmingDeleteId = user.id">
                Delete
              </Button>
            </div>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
