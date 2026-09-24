// Renders public/favicon.svg into the PNG icons a PWA needs (manifest icons and the iOS
// home-screen icon). Re-run after changing the favicon:  npm run generate-pwa-icons
//
//   pwa-192x192.png / pwa-512x512.png   the favicon as-is, with transparent rounded corners
//   maskable-512x512.png                full-bleed square with the favicon inside the safe zone,
//                                       because Android crops maskable icons to a circle/squircle
//   apple-touch-icon-180x180.png        full-bleed and opaque: iOS applies its own rounding and
//                                       paints transparent pixels black
import { readFile, writeFile } from 'node:fs/promises'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { chromium } from 'playwright'

const publicDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../public')
const favicon = (await readFile(path.join(publicDir, 'favicon.svg'), 'utf8')).replace(/<\?xml[^>]*\?>/, '').trim()

// Same stops and corner glow as the favicon's own tile, drawn across the whole canvas for the
// full-bleed variants.
const BACKGROUND = 'radial-gradient(circle at 28% 22%, rgba(255,255,255,0.35), rgba(255,255,255,0) 75%), linear-gradient(135deg, #4c1d95, #4338ca 55%, #0ea5e9)'

// The favicon without its rounded background tile (gradient + glow rects), so the cards and badge
// sit straight on the full-bleed background instead of showing a tile-inside-a-tile edge.
const artwork = favicon.replace(/<rect width="64" height="64" rx="14" fill="url\(#bg(?:Grad|Glow)\)"\/>\s*/g, '')
if (artwork === favicon) throw new Error('favicon.svg no longer has the background rects this script strips; update the pattern')

const sized = (size, svg = favicon) => svg.replace('<svg ', `<svg width="${size}" height="${size}" style="display:block" `)

const plain = size => `<div style="width:${size}px;height:${size}px">${sized(size)}</div>`

const fullBleed = (size, insetPercent) => `
  <div style="position:relative;width:${size}px;height:${size}px;background:${BACKGROUND}">
    <div style="position:absolute;inset:${insetPercent}%">${sized(size * (1 - (insetPercent * 2) / 100), artwork)}</div>
  </div>`

const icons = [
  { file: 'pwa-192x192.png', size: 192, html: plain(192), transparent: true },
  { file: 'pwa-512x512.png', size: 512, html: plain(512), transparent: true },
  // Content stays inside the central 80% (the maskable "safe zone").
  { file: 'maskable-512x512.png', size: 512, html: fullBleed(512, 10), transparent: false },
  { file: 'apple-touch-icon-180x180.png', size: 180, html: fullBleed(180, 6), transparent: false },
]

const browser = await chromium.launch()
try {
  for (const icon of icons) {
    const page = await browser.newPage({ viewport: { width: icon.size, height: icon.size } })
    await page.setContent(`<!doctype html><html><body style="margin:0;background:transparent">${icon.html}</body></html>`)
    const png = await page.screenshot({ omitBackground: icon.transparent, clip: { x: 0, y: 0, width: icon.size, height: icon.size } })
    await writeFile(path.join(publicDir, icon.file), png)
    console.log(`wrote public/${icon.file} (${png.length} bytes)`)
    await page.close()
  }
}
finally {
  await browser.close()
}
