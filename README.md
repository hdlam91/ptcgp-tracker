# Pokémon TCG Pocket Collection Tracker

Track which Pokémon TCG Pocket cards you own, see your progress per set, and manage a wishlist and trade list you can share with other players.

- **Collection tracking** per set, with progress bars and diamond / star / crown completion.
- **Search and filters** across every card: name, attack and ability text, set, pack, rarity, card type, Pokémon type, evolution stage, ability, and attack energy.
- **Card pages** with HP, attacks and energy costs, abilities, trainer text and other prints.
- **Wishlist and trade list**, with an optional public read-only share link (`/share/your-name`).
- **Installable on your phone** as a PWA (needs HTTPS).
- **Accounts** for multiple users, plus an admin **Settings** page (manage users, moderate share links, open or close registration).

See [CLAUDE.md](./CLAUDE.md) for the architecture and coding conventions.

## Stack

- Frontend: Vue 3 + TypeScript + Vite + Tailwind CSS + shadcn/ui
- Backend: ASP.NET Core Minimal APIs (Backend-for-Frontend)
- Database: PostgreSQL + EF Core
- Card data: [pokemon-tcg-pocket-cards](https://github.com/PocketDecks/pokemon-tcg-pocket-cards), pinned to a fixed release

## Requirements

To **run** the app you only need:

- [Docker](https://docs.docker.com/get-docker/) with Docker Compose
- Internet access on first start. The backend downloads the card data from GitHub, and card images are loaded from GitHub too, unless you store them on your own server (see [Card art on your own server](#card-art-on-your-own-server)).

To **develop** on it you also need:

- [Node.js](https://nodejs.org/) 22 (matches the frontend Docker image)
- [.NET SDK](https://dotnet.microsoft.com/download) 10, only if you want to run the backend tests

## Self-hosting with just the compose file

You don't need the source code, a `.env`, or anything else on your server. Save this as `docker-compose.yml` (it is also in this repository as `docker-compose.selfhost.yml`):

```yaml
name: ptcgp-tracker-selfhost

services:
  postgres:
    image: postgres:17-alpine
    restart: unless-stopped
    environment:
      POSTGRES_USER: ptcgp
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-changeme}   # change before the first start
      POSTGRES_DB: ptcgp_tracker
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ptcgp -d ptcgp_tracker"]
      interval: 5s
      timeout: 5s
      retries: 10

  backend:
    image: ghcr.io/hdlam91/ptcgp-tracker-backend:${PTCGP_VERSION:-latest}
    restart: unless-stopped
    depends_on:
      postgres:
        condition: service_healthy
    environment:
      ConnectionStrings__Default: "Host=postgres;Port=5432;Database=ptcgp_tracker;Username=ptcgp;Password=${POSTGRES_PASSWORD:-changeme}"
      Admin__Emails: ${ADMIN_EMAILS:-}                    # your email, so you become admin
      APPLY_MIGRATIONS_ON_STARTUP: "true"
      ASPNETCORE_URLS: "http://+:8080"
    volumes:
      - images:/data/images                               # card art downloaded from Settings

  frontend:
    image: ghcr.io/hdlam91/ptcgp-tracker-frontend:${PTCGP_VERSION:-latest}
    restart: unless-stopped
    depends_on:
      - backend
    ports:
      - "${PTCGP_PORT:-8081}:80"                          # the website

volumes:
  pgdata:
  images:
```

Then, in the same folder, set your options and start it. Each option is either an environment variable or a value you edit in the file:

```bash
export POSTGRES_PASSWORD='a-long-random-password'   # set before the first start
export ADMIN_EMAILS='you@example.com'                # you become admin when you register with this email
export PTCGP_PORT=8081                               # optional: the port the website listens on

docker compose up -d
```

Open `http://<your-server>:8081` and **register with the email you put in `ADMIN_EMAILS` straight away**. Emails aren't verified, so whoever registers that address first gets the admin role. Once your account exists you can close registration from Settings.

Only the website port is published. The database and API are reachable from inside the compose project only.

To update later: `docker compose pull && docker compose up -d`. Your data lives in the `pgdata` volume and survives updates. To pin a release instead of `latest`, set `PTCGP_VERSION` (for example `1.2.0` or `sha-abc1234`).

> **The environment variables must be set every time you run `docker compose`.** Exported variables only last for your shell session. For a permanent setup, edit the values straight into the file (replace `${ADMIN_EMAILS:-}` with `you@example.com`, and so on), or put them in a `.env` file next to it.

### Publishing the images (maintainers)

The compose file pulls images from GitHub Container Registry. They are built and published by [`.github/workflows/publish-images.yml`](.github/workflows/publish-images.yml) on every push to `master`, and on version tags like `v1.2.0`. They are built for `amd64` and `arm64`, so they also run on ARM servers.

1. Push this repository to GitHub as `hdlam91/ptcgp-tracker` (image names are `ghcr.io/hdlam91/ptcgp-tracker-backend` and `-frontend`; change them in the workflow and compose file if your account or repository differs).
2. Wait for the **Publish images** action to finish.
3. **Make the two packages public**: GitHub → your profile → Packages → each package → Package settings → Change visibility. GitHub creates them private, and a server can't pull private images without logging in. (Or keep them private and run `docker login ghcr.io` on the server with a token that has `read:packages`.)

## Quick start (build from source)

If you'd rather build the images yourself from a checkout, no `.env` is needed either. Every setting has a default in `docker-compose.yml`:

```bash
git clone <this repository>
cd ptcgp-tracker

docker compose up --build -d
```

Then open **http://localhost:8081** and create an account. To set your own options (a database password, your admin email), export them as variables or copy `.env.example` to `.env` and fill it in. See [Configuration](#configuration).

The first start takes a little while: it builds the images, creates the database and applies the migrations. The card data is downloaded once the backend is up.

Useful commands:

```bash
docker compose logs -f backend   # watch the backend start (look for "Card catalog cache populated")
docker compose down              # stop everything, keep your data
docker compose down -v           # stop and DELETE the database (all accounts and collections)
```

> **Set `POSTGRES_PASSWORD` before the first start.** Postgres only reads it when it creates its data volume. Changing it later won't change the password of an existing database. Either change it inside Postgres, or run `docker compose down -v` to start fresh.

## Configuration

Every option has a default, so nothing is required. You can set them as environment variables, or in a `.env` file next to the compose file (copy `.env.example`; it is gitignored).

| Variable | Default | Purpose |
| --- | --- | --- |
| `POSTGRES_PASSWORD` | `changeme` | Database password. Set it before the first start. |
| `POSTGRES_USER`, `POSTGRES_DB` | `ptcgp`, `ptcgp_tracker` | Database user and name (build-from-source compose only; the self-host file fixes them). |
| `ADMIN_EMAILS` | *(none)* | Comma-separated emails that get the admin role (see [Your first admin](#your-first-admin)). |
| `PTCGP_PORT` | `8081` | Host port for the website (self-host file). |
| `PTCGP_VERSION` | `latest` | Which published image release to run (self-host file). |
| `CARD_DATA_REPO_TAG` | `v5.3.1` | The pinned `pokemon-tcg-pocket-cards` release the backend downloads (build-from-source compose). See [Updating the card data](#updating-the-card-data). |

Ports used by the build-from-source `docker-compose.yml`:

| Port | What |
| --- | --- |
| `8081` | The website (nginx serving the frontend and proxying `/api` to the backend) |
| `8080` | The backend API |
| `5433` | PostgreSQL (`5433` rather than `5432`, to avoid clashing with a Postgres you may already run) |

The self-host file publishes only the website port.

## Your first admin

**Nobody is an admin by default.** The first person to register does *not* become one, and there is no button that creates the first admin. You choose them with `ADMIN_EMAILS`:

1. Set `ADMIN_EMAILS=you@example.com` (in your environment, in `.env`, or in the compose file; several emails are allowed, separated by commas).
2. Start the app and **register with that email**. You are an admin from your first request.

Emails aren't verified, so whoever registers a listed address first becomes admin. Register your own account right away, before you tell anyone else about the site.

If you registered *before* adding your email, add it and recreate the backend so it picks up the change:

```bash
docker compose up -d backend
```

Admins see a gear icon in the header that opens **Settings**, where they can:

- search users, promote or demote admins, and delete a user together with their data
- see who has a public share link and disable it
- **close or open registration** so no new accounts can be created
- view and refresh the server's copy of the card data
- **download all card art to your server**, so the site stops depending on GitHub for images (see below)

More admins can be added from that page. You can't change or delete your own account there, so a mistake can't remove the last admin. `ADMIN_EMAILS` only ever *adds* admins: if you demote someone who is still listed, they become admin again the next time the backend starts.

## Card art on your own server

By default the card and pack pictures are not stored on your server. Each visitor's browser loads them from GitHub, so the art needs GitHub to be reachable from their device, and a network that blocks it shows blank cards.

To store the art yourself, sign in as an admin and open **Settings → Card images → Download card images**. The server downloads every card and pack image from the pinned release into its `images` volume (about 135 MB, roughly a minute on a normal connection) and shows the progress. From then on the site serves the pictures itself.

- **It's safe to run again.** **Download missing images** only fetches what isn't stored yet, so it's the button to press after updating the card data or if a few downloads failed. Files already stored are never downloaded again.
- **Anything not stored still works.** If a picture isn't on the server (or the backend can't be reached), the app falls back to GitHub for that one picture. The promo packs have no art of their own and always load from GitHub.
- **Where it lives.** The `images` volume in the compose files. It only holds copies of public images, so you never need to back it up. To remove the copy, stop the stack and delete that volume (`docker volume rm <project>_images`, the project being your compose folder or `ptcgp-tracker-selfhost`).
- **Downloading needs GitHub once**, from the server. After that the server doesn't need it for art.

## Installing it on your phone

The app is a PWA, so you can add it to your home screen and open it like a normal app, full screen and with its own icon.

**It needs HTTPS.** Browsers only offer to install a web app from a secure address. `http://your-server:8081` won't get the install option on Android, and on iPhones it only makes a plain shortcut. `http://localhost` is the one exception, which is only useful for testing on the same computer. So put your server behind a reverse proxy that serves HTTPS on a domain name you own. [Caddy](https://caddyserver.com/) does this with almost no setup, because it gets and renews the certificate for you:

```
# /etc/caddy/Caddyfile
tracker.example.com {
    encode zstd gzip
    reverse_proxy localhost:8081
}
```

Point `tracker.example.com` at your server first, and make sure ports 80 and 443 are open. Any other HTTPS proxy or tunnel works too.

- **Keep `encode`.** The app's card data is a single 4.3 MB file and the app's own web server doesn't compress it. With `encode` it is about 450 KB, which is what your phone downloads on first install.
- **Use a domain or subdomain of its own**, not a path such as `example.com/tracker`. The app expects to live at the root of its address.
- **If Caddy runs in Docker**, `localhost:8081` would point at Caddy's own container. Put Caddy on the same compose network and use `reverse_proxy frontend:80`, or use your server's address instead of `localhost`.
- **With Caddy in front, don't leave the plain HTTP port open.** In the compose file change `"${PTCGP_PORT:-8081}:80"` to `"127.0.0.1:8081:80"`, so only Caddy (on the same machine) can reach it. Docker publishes ports around most host firewalls, so don't rely on one to block it.

Then, on your phone, open your `https://` address and:

- **Android (Chrome):** menu (⋮) → **Install app** (or **Add to Home screen**). Chrome may also offer it on its own.
- **iPhone (Safari):** Share button → **Add to Home Screen**. It has to be Safari; other iOS browsers can't install web apps.

A few things to know:

- It still needs a connection. Your collection lives on the server, so opening the app with no network shows a "Can't reach the server" screen with a **Try again** button. The install makes it launch faster and feel like an app; it doesn't add offline use.
- Updates are automatic. After you update the server, the installed app picks up the new version the next time it opens and reloads once.
- The app icons are generated from `frontend/public/favicon.svg`. After changing the favicon, run `npm run generate-pwa-icons` (in `frontend/`) to refresh them.
- The service worker is only built into production builds. `npm run dev` doesn't have it, so to try the install behaviour locally use the Docker frontend (`http://localhost:8081`).

## Development

Run the database and backend in Docker, and the frontend with Vite so you get hot reload:

```bash
docker compose up -d postgres backend

cd frontend
npm install
npm run dev
```

The dev site is at **http://localhost:5173**. Vite proxies `/api` to the backend on port 8080, so the browser only ever talks to one origin and cookie login works without any CORS setup. In production nginx does the same job.

After changing backend code, rebuild it:

```bash
docker compose up -d --build backend
```

### Local card images (optional)

By default card and pack art is loaded from GitHub. To serve it from disk instead in development:

```bash
cd frontend
npm run download-card-images
```

This is only for `npm run dev`; a running server stores its art from the admin Settings page instead (see [Card art on your own server](#card-art-on-your-own-server)). It copies the images into `frontend/public/card-images` and `frontend/public/pack-images`. It is safe to re-run and only downloads what is missing. The folders are gitignored and are not included in Docker images. If a local file is missing, the app falls back to the GitHub URL.

### Tests

```bash
# Backend: needs Docker, because it starts a throwaway Postgres container
cd backend
dotnet test

# Frontend end-to-end tests (Playwright)
cd frontend
npx playwright install chromium   # once
npm run test:e2e
```

The end-to-end tests run against your **running dev stack** (`docker compose up -d postgres backend` plus `npm run dev`) and create real test users through the API. So:

- **Registration must be open**, or the tests can't create users.
- The admin tests promote their users by running `psql` inside the `postgres` container, so they need `docker compose` to work from the repository root.

The PWA checks (`e2e/pwa.spec.ts`) need a production build, so they are skipped unless you point them at the Docker frontend: `E2E_BASE_URL=http://localhost:8081 E2E_PWA=1 npx playwright test e2e/pwa.spec.ts` (rebuild it first with `docker compose up -d --build frontend`).

If an admin has closed registration and you only want to run read-only specs, set `E2E_LOGIN_EMAIL` and `E2E_LOGIN_PASSWORD` to sign in as an existing account instead of registering. Only do that for tests that don't change its data.

### Updating the card data

The card data is pinned so a new expansion can't change IDs under you. To move to a new release, update every place the version is written, then rebuild:

1. `frontend/`: `npm install pokemon-tcg-pocket-cards@<version>`
2. `backend/src/PtcgpTracker.Api/appsettings.json`: `CardData:RepoTag` (`v<version>`). This is the default baked into the published backend image.
3. `docker-compose.yml`: the `CARD_DATA_REPO_TAG` default (and `.env.example` if you use it).

If the frontend and backend versions drift apart, cards the backend doesn't know about are rejected when you try to save them.

## Project layout

```
backend/     ASP.NET Core API, EF Core migrations, and xUnit tests
frontend/    Vue app, its nginx config and Dockerfile, and Playwright tests (e2e/)
docker-compose.yml           builds and runs everything from source (no .env needed)
docker-compose.selfhost.yml  runs prebuilt images: the only file a server needs
.github/workflows/           publishes the images to GitHub Container Registry
.env.example                 optional configuration template
CLAUDE.md            architecture and conventions
```

The frontend never talks to the database. The backend owns all collection and trade-list data, and returns only *which cards you own*. Card names, art and stats come from the pinned dataset bundled into the frontend.

## Before you expose it to other people

The compose file is set up for local use or a trusted network. If other people will reach it over the internet:

- set a strong `POSTGRES_PASSWORD`. The self-host compose file already publishes only the website port; if you build from source, remove the published `5433` (database) and `8080` (API) ports from `docker-compose.yml`
- put a reverse proxy that terminates HTTPS in front of the website port (this is also what lets people [install it on their phones](#installing-it-on-your-phone))
- register your admin account before anyone else can (see [Your first admin](#your-first-admin))
- decide whether strangers should be able to sign up. You can close registration from the admin Settings page once your own account exists.
- the backend applies database migrations on startup (`APPLY_MIGRATIONS_ON_STARTUP`). That is convenient for one instance, but if you run several copies, apply migrations as a separate step instead.

## Troubleshooting

- **"Registration is closed" when signing up:** an admin closed it. Open it again from Settings, or ask an admin.
- **Card pictures are blank or missing:** they load from GitHub in each visitor's browser, so this usually means GitHub is blocked or unreachable from that device. Store the art on your server (see [Card art on your own server](#card-art-on-your-own-server)) to avoid the dependency.
- **Cards show, but saving your collection fails ("Unknown cardId"):** the backend hasn't loaded its card data. Check `docker compose logs backend` for "Card catalog cache populated"; the backend needs to reach GitHub to download it. Admins can retry from Settings → Card data → Refresh.
- **Port already in use:** set `PTCGP_PORT` (self-host file), or change the left-hand port numbers in `docker-compose.yml`.
- **`docker compose pull` says "denied" or "unauthorized":** the GHCR packages are still private. Make them public, or `docker login ghcr.io` (see [Publishing the images](#publishing-the-images-maintainers)).
- **You forgot which account is admin:** admins have a gear icon in the header. To promote someone without another admin, add their email to `ADMIN_EMAILS` and restart the backend.

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
