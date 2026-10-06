import { ref } from 'vue'

export interface HoverPreviewOptions {
  width: number
  height: number
}

/**
 * Tracks a mouse-only hover state and a cursor-follow position for an enlarged preview — shared
 * by PackThumbnail and CardArt, which each show their own enlarged image near the pointer on
 * hover. Sits to the lower right of the cursor, flipping to the other side near a viewport edge.
 */
export function useHoverPreview(options: HoverPreviewOptions) {
  const CURSOR_GAP = 16

  const hovering = ref(false)
  const position = ref({ left: 0, top: 0 })

  function onPointerMove(event: PointerEvent) {
    // Hover previews are a mouse affordance; on touch, a tap shouldn't leave one stuck open.
    if (event.pointerType !== 'mouse')
      return
    hovering.value = true

    const fitsRight = event.clientX + CURSOR_GAP + options.width <= window.innerWidth
    const fitsBelow = event.clientY + CURSOR_GAP + options.height <= window.innerHeight
    position.value = {
      left: fitsRight ? event.clientX + CURSOR_GAP : Math.max(0, event.clientX - CURSOR_GAP - options.width),
      top: fitsBelow ? event.clientY + CURSOR_GAP : Math.max(0, event.clientY - CURSOR_GAP - options.height),
    }
  }

  function onPointerLeave() {
    hovering.value = false
  }

  return { hovering, position, onPointerMove, onPointerLeave }
}
