import { ref } from 'vue'

/**
 * Local-cache-first image src with a fallback to a remote URL on load error.
 * Covers every reason the local copy might not be there: it was never
 * downloaded, the download failed, or (in a deployed container) the local
 * cache is dev-only and doesn't exist at all — see scripts/download-card-images.mjs.
 */
export function useLocalImage(localUrl: string | undefined, remoteUrl: string | null | undefined) {
  const src = ref(localUrl ?? remoteUrl ?? undefined)

  function onError() {
    // First failure: the local copy is missing (or the whole cache is dev-only
    // and absent) — fall back to the remote URL. Second failure: even the
    // remote URL is bad — give up and hide the image rather than show a
    // broken-image icon.
    src.value = src.value === localUrl && remoteUrl ? remoteUrl : undefined
  }

  return { src, onError }
}
