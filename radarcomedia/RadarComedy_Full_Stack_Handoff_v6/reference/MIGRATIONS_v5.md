# Database migration guide

## Migration files

- `db/V001__initial_schema.sql`: Initial application tables, foreign keys, status and price constraints, geo columns, indexes. Executes inside a transaction and enables PostGIS.
- `db/V002__public_eligibility.sql`: Creates `public_occurrences`, a read-only public-discovery projection that excludes all unpaid/unapproved/suppressed/canceled records.
- `db/V003__comedian_public_profiles.sql`: Adds moderated comedian profile metadata, status and verified social links, plus lineup lookup index. Apply after V002.

**This package does not contain a deployed database or a generated EF Core `ModelSnapshot`.** The versioned PostgreSQL scripts are reference migrations and can be applied with an appropriate reviewed SQL migration runner (DbUp/Flyway) or `psql`. The C# EF Core entities/DbContext are a typed starting implementation; reconcile their relational metadata and generate EF model snapshots before switching to EF-native migrations. **Do not run SQL and a separately generated EF `InitialCreate` against the same database.** Pick a single migration owner first.

## Suggested dev database

- Postgres 17 image with a matching PostGIS extension, for example an official/approved PostGIS-enabled distribution, with a pinned image digest in the final Compose file.
- DB: `radarcomedy`; application user cannot create schemas/extensions in production; migration runner uses a separate privileged account.
- Connection string via secrets/environment, never in repo.
- Use least-privilege credentials for public read vs promoter/admin write if feasible.

## Applying SQL reference migrations manually in local dev

```bash
psql "$RADAR_DATABASE_URL" -v ON_ERROR_STOP=1 -f db/V001__initial_schema.sql
psql "$RADAR_DATABASE_URL" -v ON_ERROR_STOP=1 -f db/V002__public_eligibility.sql
psql "$RADAR_DATABASE_URL" -v ON_ERROR_STOP=1 -f db/V003__comedian_public_profiles.sql
```

These commands are for a **fresh** dev database. Do not blindly replay V001. Integrate with migration history bookkeeping for staging and production, and back up before changes.

## EF Core config sketch

```csharp
builder.Services.AddDbContext<RadarDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Radar"),
        npgsql => npgsql.UseNetTopologySuite()));
```

Use `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite` matching your EF Core major version. Reference CLR entities in `src/RadarComedy.Domain`; the sample uses `DateOnly`/`TimeOnly` and UTC `DateTime`. All `DateTime` values persisted to `timestamptz` must be `DateTimeKind.Utc`.

## Path to EF-native migration ownership (if you prefer dotnet ef)

1. Build and validate `RadarDbContext` and domain model against local PostgreSQL 17/PostGIS using .NET 10 SDK and matching EF10/Npgsql10 dependencies.
2. On a **fresh separate** database, generate `dotnet ef migrations add InitialCreate -p src/RadarComedy.Infrastructure -s <api-project>` and inspect the generated C# migration and model snapshot. The API project must exist and reference Infrastructure. `dotnet ef` cannot be run against this handoff alone because an API startup/design-time factory is not included.
3. Compare its generated SQL with V001/V002/V003, then incorporate the required CHECK constraints, partial indexes, composite FK, view and other database-only objects via `migrationBuilder.Sql()`.
4. Validate against the acceptance test suite and use **the EF migrations instead of V001/V002**, not in addition. Keep the generated snapshot committed to source control.
5. Deploy only after SQL migration review and restore drill.

## Queries to sanity-check schema

```sql
SELECT extname FROM pg_extension WHERE extname='postgis';
SELECT table_name FROM information_schema.tables WHERE table_schema='public' ORDER BY table_name;
SELECT * FROM public_occurrences LIMIT 0;
SELECT indexname FROM pg_indexes WHERE tablename='event_occurrences';
```

## Rollback recommendation

Prefer forward correction migrations; do not destroy financial/moderation history. For local **throwaway** DB only, drop the `public_occurrences` view, application tables in FK reverse order (or recreate the database). In production, restore a tested backup under controlled procedures rather than dropping paid orders. Keep the PostGIS extension unless the entire database is decommissioned.

## Important limitations of the sample

- No database has been provisioned or migrated by this handoff. No `dotnet build`/`dotnet test` was run because the .NET SDK is not present in the authoring environment.
- This is a carefully specified implementation scaffold, not a claim of compiled production-ready API services.
- On day one, implement reconciliations and idempotency for simultaneous Stripe sessions; even with a unique `Paid` order, a payer can be charged twice externally if two checkout sessions succeed. Duplicate external charges must trigger refunds/escalation, never silent data loss.
