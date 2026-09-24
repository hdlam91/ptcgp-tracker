import { computed, ref } from 'vue'
import { adminService } from '@/services/adminService'
import { ApiError } from '@/services/httpClient'
import type { AdminUserResponse, CatalogStatusResponse } from '@/types/api'

function describe(error: unknown): string {
  if (error instanceof ApiError) {
    const body = error.body as { error?: string, detail?: string } | null
    return body?.error ?? body?.detail ?? `Request failed (${error.status}).`
  }
  return 'Something went wrong. Please try again.'
}

/** State and actions for the admin settings page. Local to the page, not a shared singleton. */
export function useAdmin() {
  const users = ref<AdminUserResponse[]>([])
  const search = ref('')
  const registrationOpen = ref<boolean | null>(null)
  const catalog = ref<CatalogStatusResponse | null>(null)
  const loading = ref(true)
  const error = ref('')
  const busy = ref(false)

  const filteredUsers = computed(() => {
    const term = search.value.trim().toLowerCase()
    if (!term) return users.value
    return users.value.filter(u => u.displayName.toLowerCase().includes(term) || u.email.toLowerCase().includes(term))
  })

  async function run(action: () => Promise<void>) {
    error.value = ''
    busy.value = true
    try {
      await action()
    }
    catch (e) {
      error.value = describe(e)
    }
    finally {
      busy.value = false
    }
  }

  async function load() {
    try {
      const [loadedUsers, settings, loadedCatalog] = await Promise.all([
        adminService.users(),
        adminService.getSettings(),
        adminService.catalog(),
      ])
      users.value = loadedUsers
      registrationOpen.value = settings.registrationOpen
      catalog.value = loadedCatalog
    }
    catch (e) {
      error.value = describe(e)
    }
    finally {
      loading.value = false
    }
  }

  const toggleRegistration = () => run(async () => {
    const next = !registrationOpen.value
    registrationOpen.value = (await adminService.updateSettings(next)).registrationOpen
  })

  const setAdmin = (user: AdminUserResponse, isAdmin: boolean) => run(async () => {
    await adminService.setAdmin(user.id, isAdmin)
    user.isAdmin = isAdmin
  })

  const deleteUser = (user: AdminUserResponse) => run(async () => {
    await adminService.deleteUser(user.id)
    users.value = users.value.filter(u => u.id !== user.id)
  })

  const disableShare = (user: AdminUserResponse) => run(async () => {
    await adminService.disableShare(user.id)
    user.shareHandle = null
  })

  const refreshCatalog = () => run(async () => {
    catalog.value = await adminService.refreshCatalog()
  })

  return {
    users,
    filteredUsers,
    search,
    registrationOpen,
    catalog,
    loading,
    error,
    busy,
    load,
    toggleRegistration,
    setAdmin,
    deleteUser,
    disableShare,
    refreshCatalog,
  }
}
