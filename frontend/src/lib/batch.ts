/**
 * Runs `fn` over `items` in fixed-size concurrent chunks, awaiting each chunk
 * before starting the next. Used for bulk actions (e.g. "mark all owned") that
 * can involve hundreds of individual API calls — chunking keeps a handful of
 * requests in flight at once instead of firing everything in one burst.
 */
export async function runInBatches<T>(items: T[], batchSize: number, fn: (item: T) => Promise<unknown>): Promise<void> {
  for (let i = 0; i < items.length; i += batchSize) {
    const batch = items.slice(i, i + batchSize)
    await Promise.all(batch.map(fn))
  }
}
