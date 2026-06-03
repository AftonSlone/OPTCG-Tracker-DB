# OPTCG Tracker Database

Dockerized PostgreSQL + .NET importer that pulls all One Piece TCG card data
from [optcgapi.com](https://optcgapi.com) into a single `cards` table.

## What you need

Just **Docker** (Desktop or Engine + Compose v2). Verify your install with:

```bash
docker --version
docker compose version
```

The .NET SDK is only required if you want to add new EF Core migrations — it is
**not** needed to run anything.

## First-time local setup

Follow these steps in order the first time you clone the repo.

### 1. Create your env file

Copy the example file and open it for editing:

```bash
# macOS / Linux
cp .env.example .env

# Windows (PowerShell)
Copy-Item .env.example .env
```

Then set a password for `POSTGRES_PASSWORD` (and optionally `PGADMIN_PASSWORD`).

> **Important — avoid `$` in passwords.** Docker Compose treats `$` in `.env`
> values as a variable reference, **even inside quotes**. A password like
> `Ab$cd` becomes `Ab` plus an empty variable, which silently breaks auth and
> prints `variable is not set` warnings. Either avoid `$` entirely, or escape
> each one as `$$` (e.g. `Ab$$cd`). Characters like `@`, `&`, `!`, `%` are safe.

The variables in `.env`:

| Variable | Used by | Notes |
|---|---|---|
| `POSTGRES_USER` | db + importer | Database role name (default `optcg`) |
| `POSTGRES_PASSWORD` | db + importer | Set this; no `$` |
| `POSTGRES_DB` | db + importer | Database name (default `optcg`) |
| `POSTGRES_PORT` | db | Host port mapped to Postgres (default `5432`) |
| `PGADMIN_EMAIL` | pgAdmin | Login email for the pgAdmin UI |
| `PGADMIN_PASSWORD` | pgAdmin | Login password for the pgAdmin UI |
| `PGADMIN_PORT` | pgAdmin | Host port for the pgAdmin UI (default `8080`) |

### 2. Start the database

```bash
docker compose up -d --build
```

The `importer` is gated behind the `import` profile, so `docker compose up`
starts **only** the database. Confirm it is healthy before continuing:

```bash
docker compose ps
# db should show STATUS "running (healthy)"
```

### 3. Run the card importer

```bash
docker compose run --rm importer
```

This starts a one-shot job that waits for PostgreSQL to become healthy, applies
EF Core migrations (`Database.Migrate()`), fetches all cards, and upserts them.
Re-running picks up newly released cards without creating duplicates
(idempotent). Prices are intentionally not imported, tracked, or stored.

### 4. Verify the data

```bash
docker compose exec db psql -U optcg -d optcg -c "SELECT COUNT(*) FROM cards;"
```

You should see a non-zero card count.

### Card images

The importer automatically downloads card images from optcgapi.com into the
`card-images/` folder, organized by set subfolder (e.g. `card-images/OP01/OP01-001.png`).

- Images are downloaded **only if the card still has a remote URL** in the database
  (first run or after a fresh import). If the file already exists locally, it's skipped.
- The `CardImageUrl` column stores the **relative path** (e.g. `card-images/OP01/OP01-001.png`).
- The `IMAGE_BASE_URL` env var configures where images are served:
  - **Local:** set to your static file server (e.g. `http://localhost:8081`).
  - **AWS:** set to your CloudFront domain (e.g. `https://d123.cloudfront.net`).
- The `card-images/` folder is gitignored so large image files don't bloat the repo.
  New developers get images automatically on their first import run.

## Optional: pgAdmin (database UI)

Start the pgAdmin container (gated behind the `tools` profile):

```bash
docker compose --profile tools up -d pgadmin
```

Open <http://localhost:8080> and log in with the `PGADMIN_EMAIL` /
`PGADMIN_PASSWORD` values from your `.env`. Then register the database server
(**Servers → Register → Server...**):

| Field | Value |
|---|---|
| Name | `optcg` (any label) |
| Host name/address | `db` (the compose service name, **not** `localhost`) |
| Port | `5432` |
| Maintenance database | value of `POSTGRES_DB` (default `optcg`) |
| Username | value of `POSTGRES_USER` (default `optcg`) |
| Password | value of `POSTGRES_PASSWORD` |

> Use `db` as the host, not `localhost`. pgAdmin runs inside the Docker network
> where Postgres is reachable by its service name; `localhost` would point at the
> pgAdmin container itself.

## Common tasks & troubleshooting

- **Stop everything (keep data):** `docker compose --profile tools down`
- **Stop and wipe the database volume (deletes all data):** `docker compose down -v`
- **Change `POSTGRES_PASSWORD` without losing data:** update the live role first,
  then update `.env` and restart:

  ```bash
  docker compose exec db psql -U optcg -d optcg \
    -c "ALTER USER optcg WITH PASSWORD 'new_password_here';"
  docker compose up -d --force-recreate db
  ```

- **`variable is not set` warnings:** a `$` in a `.env` value is being
  interpolated — see the password note above.
- **pgAdmin login fails after changing `PGADMIN_PASSWORD`:** pgAdmin only applies
  that password on first init and persists its account in an anonymous volume.
  Recreate it fresh: `docker compose --profile tools up -d --renew-anon-volumes pgadmin`.

## Architecture

```
optcgapi.com  ->  OPTCG.Tracker.Importer (container)  ->  PostgreSQL (container)
                        |
                        +-- OPTCG.Tracker.Data (EF Core models + migrations)
```

- **OPTCG.Tracker.Data** — `Card` entity, `TrackerDbContext`, migrations.
- **OPTCG.Tracker.Importer** — console app: API client, JSON DTOs, import service.

### Key data notes

- The unique natural key is **`CardImageId`** (e.g. `OP01-001` vs `OP01-001_p1`),
  not `CardSetId` — base and parallel printings share the same `CardSetId`.
- Numeric API fields (`life`, `card_cost`, `card_power`, `counter_amount`) arrive
  as JSON strings or null and are parsed to `int?`.
- **Prices are not imported or stored** — the importer only syncs card catalog data.
- Don!! cards are intentionally out of scope for Phase 1.

## Endpoints imported

- `/api/allSetCards/`
- `/api/allSTCards/`
- `/api/allPromos/`

## Tech stack

- PostgreSQL 16
- .NET 10 / Entity Framework Core 10 (Npgsql provider)

## Adding a migration (requires local .NET SDK)

```bash
dotnet ef migrations add <Name> \
  --project src/OPTCG.Tracker.Data \
  --startup-project src/OPTCG.Tracker.Data
```

Migrations are applied automatically at container startup.
