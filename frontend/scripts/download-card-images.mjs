// One-off/rerunnable job: mirrors each card's and pack's image from the pinned
// pokemon-tcg-pocket-cards dataset into public/card-images and public/pack-images,
// so the app can serve them locally instead of hotlinking on every page load.
// Idempotent — already downloaded files are skipped, so it's safe to rerun after
// a dataset bump (only new/changed images get fetched). Not committed to git or
// baked into deployed images; see .gitignore and .dockerignore.
import { access, mkdir, writeFile } from 'node:fs/promises'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
// Node's ESM loader (unlike Vite, which the rest of the app goes through) needs
// an explicit type attribute to import this package's JSON data directly.
import cards from 'pokemon-tcg-pocket-cards/v5/collection' with { type: 'json' }
import expansions from 'pokemon-tcg-pocket-cards/v5/expansions' with { type: 'json' }

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const CONCURRENCY = 16

async function fileExists(filePath) {
  try {
    await access(filePath)
    return true
  }
  catch {
    return false
  }
}

async function downloadImage(item, outputDir, stats) {
  const url = item.image ?? item.image_png
  if (!url) {
    stats.skippedNoUrl++
    return
  }

  const ext = path.extname(new URL(url).pathname) || '.webp'
  const dest = path.join(outputDir, `${item.id}${ext}`)

  if (await fileExists(dest)) {
    stats.alreadyPresent++
    return
  }

  try {
    const response = await fetch(url)
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`)
    }
    const buffer = Buffer.from(await response.arrayBuffer())
    await writeFile(dest, buffer)
    stats.downloaded++
  }
  catch (error) {
    stats.failed.push({ id: item.id, url, message: error instanceof Error ? error.message : String(error) })
  }
}

async function downloadAll(items, outputDir) {
  await mkdir(outputDir, { recursive: true })

  const stats = { downloaded: 0, alreadyPresent: 0, skippedNoUrl: 0, failed: [] }
  const queue = [...items]

  async function worker() {
    let item
    // eslint-disable-next-line no-cond-assign
    while ((item = queue.shift())) {
      await downloadImage(item, outputDir, stats)
    }
  }

  await Promise.all(Array.from({ length: CONCURRENCY }, worker))
  return stats
}

function report(label, stats) {
  console.log(`\n${label}`)
  console.log(`  Downloaded:      ${stats.downloaded}`)
  console.log(`  Already present: ${stats.alreadyPresent}`)
  console.log(`  Skipped (no url): ${stats.skippedNoUrl}`)
  console.log(`  Failed:          ${stats.failed.length}`)
  if (stats.failed.length > 0) {
    console.log('  The app falls back to the dataset\'s own URL for these at runtime:')
    for (const failure of stats.failed) {
      console.log(`    ${failure.id}: ${failure.message}`)
    }
  }
}

async function run() {
  const cardStats = await downloadAll(cards, path.join(__dirname, '..', 'public', 'card-images'))
  report('Cards', cardStats)

  const packs = expansions.flatMap(expansion => expansion.packs)
  const packStats = await downloadAll(packs, path.join(__dirname, '..', 'public', 'pack-images'))
  report('Packs', packStats)
}

run()
