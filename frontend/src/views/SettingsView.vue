<script setup lang="ts">
import { onMounted } from 'vue'
import CatalogCard from '@/components/admin/CatalogCard.vue'
import ImagesCard from '@/components/admin/ImagesCard.vue'
import RegistrationCard from '@/components/admin/RegistrationCard.vue'
import UsersTable from '@/components/admin/UsersTable.vue'
import { Search } from '@lucide/vue'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { useAdmin } from '@/composables/useAdmin'
import { useAuth } from '@/composables/useAuth'

const { currentUser } = useAuth()
const {
  users, filteredUsers, search, registrationOpen, catalog, images, loading, error, busy,
  load, toggleRegistration, setAdmin, deleteUser, disableShare, refreshCatalog, startImageDownload,
} = useAdmin()

onMounted(load)
</script>

<template>
  <div class="mx-auto max-w-6xl p-4 sm:p-6">
    <h1 class="text-2xl font-semibold">
      Settings
    </h1>
    <p class="mt-1 text-muted-foreground">
      Admin tools for managing users and app data.
    </p>

    <p v-if="error" role="alert" class="mt-4 rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
      {{ error }}
    </p>

    <div class="mt-6 grid gap-4 md:grid-cols-2">
      <RegistrationCard :open="registrationOpen" :busy="busy" @toggle="toggleRegistration" />
      <CatalogCard :catalog="catalog" :busy="busy" @refresh="refreshCatalog" />
      <ImagesCard :status="images" :busy="busy" @download="startImageDownload" />
    </div>

    <Card class="mt-4">
      <CardHeader>
        <CardTitle class="text-base">
          Users ({{ search.trim() ? `${filteredUsers.length} of ${users.length}` : users.length }})
        </CardTitle>
        <CardDescription>
          Manage roles, moderate public share links, or remove accounts. You can't change or delete your own account here.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <p v-if="loading" class="text-sm text-muted-foreground">
          Loading users…
        </p>
        <template v-else>
          <div class="relative mb-3 max-w-xs">
            <Search class="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input v-model="search" type="text" placeholder="Search by name or email" aria-label="Search users" class="pl-8" />
          </div>
          <p v-if="filteredUsers.length === 0" class="py-4 text-sm text-muted-foreground">
            No users match "{{ search }}".
          </p>
        </template>
        <UsersTable
          v-if="!loading && filteredUsers.length > 0"
          :users="filteredUsers"
          :current-user-id="currentUser?.id"
          :busy="busy"
          @set-admin="setAdmin"
          @disable-share="disableShare"
          @delete="deleteUser"
        />
      </CardContent>
    </Card>
  </div>
</template>
