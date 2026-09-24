# Pokémon TCG Pocket Collection Tracker

Track which Pokémon TCG Pocket cards you own, and manage a wishlist / trade list with other players.

See [CLAUDE.md](./CLAUDE.md) for the full architecture and conventions.

## Stack

- Frontend: Vue 3 + TypeScript + Vite + Tailwind CSS + shadcn/ui
- Backend: ASP.NET Core Minimal APIs (Backend-for-Frontend)
- Database: PostgreSQL + EF Core
- Card data: [pokemon-tcg-pocket-cards](https://github.com/PocketDecks/pokemon-tcg-pocket-cards), pinned to a fixed tag

## Local development

```bash
cp .env.example .env   # fill in local values
docker compose up postgres
```

Postgres is exposed on host port `5433` (not the default `5432`) to avoid clashing with other local Postgres instances. Inside the Docker network, other services still reach it at `postgres:5432`.

Backend and frontend setup instructions will be added as those projects are scaffolded.

## Legal

This is an independent, fan-made, non-commercial collection-tracking tool for *Pokémon
Trading Card Game Pocket*. It is not produced, hosted, endorsed, sponsored, or approved by
Nintendo, Creatures Inc., GAME FREAK inc., or The Pokémon Company. Pokémon, Pokémon Trading
Card Game Pocket, and all associated names, logos, and card designs are trademarks and
copyrighted material of their respective owners; this project claims no ownership over that
material.

Card names, rarities, set information, and card images are sourced from the
community-maintained [pokemon-tcg-pocket-cards](https://github.com/PocketDecks/pokemon-tcg-pocket-cards)
dataset, pinned to a fixed release (see `frontend/package.json`), and are used for
identification purposes only. The same disclaimer is shown in-app at `/legal`.
