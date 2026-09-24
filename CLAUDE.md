# Pokémon TCG Pocket Collection Tracker

## Stack

Frontend:
- Vue 3
- TypeScript
- Vite
- Composition API
- Tailwind CSS
- shadcn/ui (Radix UI primitives)

Backend:
- ASP.NET Core
- Minimal APIs
- Backend-for-Frontend architecture

Database:
- PostgreSQL
- Entity Framework Core

Infrastructure:
- Docker
- Docker Compose

## Architecture

Browser
  ↓
Vue
  ↓
BFF
  ├── PostgreSQL
  └── Pokémon card dataset

The frontend must never communicate directly with PostgreSQL.

The BFF owns all access to collection data.

## Card data

Card metadata comes from:
https://github.com/PocketDecks/pokemon-tcg-pocket-cards

Pin to a specific tag/commit, never `main` — new expansion releases can shift IDs/fields.

Frontend: consume via the `pokemon-tcg-pocket-cards` npm package (typed JSON + images).

Backend (BFF): npm doesn't fit a .NET build — fetch the "collection payload" JSON variant directly from `raw.githubusercontent.com` at the pinned ref, cache it (startup load or scheduled refresh), don't run npm from the BFF.

Do not manually duplicate card metadata in PostgreSQL.

Cards are identified using the stable ID supplied by the dataset.

Images should use the source repository's images.

## Vue conventions

- Always use Vue 3 Composition API.
- Use `<script setup lang="ts">`.
- Avoid `any`.
- Extract reusable UI into components.
- Extract reusable logic into composables.
- Keep API access out of UI components.
- Use strongly typed API models.

## UI

Use the ui-ux-pro-max skill for UI/UX decisions.

Use the frontend-design skill when creating new interfaces.

The application must work well on:
- Desktop
- Tablet
- Mobile

Card images are a primary visual element.

## Testing

Use Playwright for end-to-end testing.

Important user flows must have tests.

When completing UI work:
1. Run the application.
2. Test the feature.
3. Check browser console errors.
4. Screenshot desktop.
5. Screenshot mobile.
6. Fix visual issues before considering the task complete.