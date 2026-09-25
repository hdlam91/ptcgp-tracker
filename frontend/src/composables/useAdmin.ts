import { computed, onScopeDispose, ref } from 'vue'
import { adminService } from '@/services/adminService'
import { ApiError } from '@/services/httpClient'
import type { AdminUserResponse, CatalogStatusResponse, ImageMirrorStatusResponse } from '@/types/api'

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
  const images = ref<ImageMirrorStatusResponse | null>(null)
  const loading = ref(true)
  const error = ref('')
  const busy = ref(false)

  const filteredUsers = computed(() => {
    const term = search.value.trim().toLowerCase()
    if (!term) return users.value
    return users.value.filter(u => u.displayName.toLowerCase().includes(term) || u.email.toLowerCase().includes(term))
  })

  // The image download runs on the server for a while, so watch its progress until it stops.
  let pollTimer: ReturnType<typeof setInterval> | undefined

  function stopPolling() {
    if (pollTimer !== undefined) clearInterval(pollTimer)
    pollTimer = undefined
  }

  function pollWhileRunning() {
    if (pollTimer !== undefined || images.value?.state !== 'Running') return
    pollTimer = setInterval(async () => {
      try {
        images.value = await adminService.images()
      }
      catch {
        // A blip shouldn't end the progress display; the next tick tries again.
      }
      if (images.value?.state !== 'Running') stopPolling()
    }, 1000)
  }

  onScopeDispose(stopPolling)

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
      const [loadedUsers, settings, loadedCatalog, loadedImages] = await Promise.all([
        adminService.users(),
        adminService.getSettings(),
        adminService.catalog(),
        adminService.images(),
      ])
      users.value = loadedUsers
      registrationOpen.value = settings.registrationOpen
      catalog.value = loadedCatalog
      images.value = loadedImages
      // A download started before this page was opened (or reloaded) is still worth watching.
      pollWhileRunning()
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

  const startImageDownload = () => run(async () => {
    images.value = await adminService.startImageDownload()
    pollWhileRunning()
  })

  return {
    users,
    filteredUsers,
    search,
    registrationOpen,
    catalog,
    images,
    loading,
    error,
    busy,
    load,
    toggleRegistration,
    setAdmin,
    deleteUser,
    disableShare,
    refreshCatalog,
    startImageDownload,
  }
}
