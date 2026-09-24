import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  // Every test registers a real user and hits the same single dev server +
  // backend + Postgres instance — too much worker parallelism causes enough
  // contention to occasionally exceed even a generous assertion timeout.
  // workers: 2 was fine for 7 tests but started flaking (~1 in 8 runs) once
  // the suite grew to 9; serial execution costs ~15-20s more but is reliable.
  workers: 1,
  retries: 0,
  reporter: 'list',
  // Each test registers a real user and loads the full ~3MB card catalog against a
  // single dev server + backend instance; under parallel workers that can outrun the
  // default 5s assertion timeout even though nothing is actually wrong. Give it room.
  expect: { timeout: 10_000 },
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:5173',
    trace: 'retain-on-failure',
    actionTimeout: 10_000,
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
})
