import { computed, ref } from 'vue'
import { configService } from '@/services/configService'

// null until the first load finishes, so pages can avoid flashing the wrong state.
const registrationOpen = ref<boolean | null>(null)

async function load() {
  try {
    registrationOpen.value = (await configService.get()).registrationOpen
  }
  catch {
    // The server enforces the rule anyway; if the lookup fails, don't hide sign-up.
    registrationOpen.value = true
  }
}

function markRegistrationClosed() {
  registrationOpen.value = false
}

export function useAppConfig() {
  return {
    registrationOpen: computed(() => registrationOpen.value),
    load,
    markRegistrationClosed,
  }
}
