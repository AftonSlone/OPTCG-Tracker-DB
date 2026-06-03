# OPTCG Tracker Database

Dockerized PostgreSQL + .NET importer that pulls all One Piece TCG card data
from [optcgapi.com](https://optcgapi.com) into a single `cards` table.

## What you need

Just **Docker** (Desktop or Engine + Compose). The .NET SDK is only required if
you want to add new EF Core migrations — it is **not** needed to run anything.

## Quick start

```bash
# 1. Create your env file
cp .env.example .env        # then edit POSTGRES_PASSWORD

# 2. Start the database (importer does NOT run automatically)
docker compose up -d --build

# 3. Run the card importer on demand
docker compose run --rm importer
```

The `importer` is gated behind the `import` profile, so `docker compose up`
starts only the database. Running `docker compose run --rm importer` starts a
one-shot job that waits for PostgreSQL to become healthy, applies EF Core
migrations (`Database.Migrate()`), fetches all cards, and upserts them.
Re-running picks up newly released cards without creating duplicates
(idempotent). Prices are intentionally not imported, tracked, or stored.

### Verify the data

```bash
docker compose exec db psql -U optcg -d optcg -c "SELECT COUNT(*) FROM cards;"
```

### Optional: pgAdmin

```bash
docker compose --profile tools up -d pgadmin
# then open http://localhost:8080
```

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
