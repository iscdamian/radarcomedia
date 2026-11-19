# Development, staging and production deployment runbook

**Not a deployed configuration.** Build actual app containers, ports and health endpoints from this spec. Use a **separate** Docker Compose project from other apps and separate database, storage namespace, credentials, Stripe webhook and backup schedule.

## Required services

| Service | Function | Public exposure |
|---|---|---|
| `caddy` | TLS and same-origin reverse proxy | 80/443 only |
| `web` | Next.js SSR + static PWA assets | Reverse proxy only |
| `api` | ASP.NET Core .NET 10 API | `/api/v1/*` via proxy only |
| `worker` | Outbox, expiry, newsletter, reconciliation | None |
| `postgres` | PostgreSQL + PostGIS | Private network only |
| `object-store` | Media S3-compatible | Private network / vetted signed uploads |

**Optional** external mail provider (no local outbound MTA required), hosted tile service. Postgres should run PostGIS-capable image, not plain Postgres image lacking extension. Never depend on the server's local time zone; app/DB UTC for instants, venue-local IANA for schedules.

## Environment handling

Start from `.env.example` as a variable inventory; values such as Stripe keys, database passwords, email API keys, auth signing/data-protection keys, admin seed credentials, MinIO keys and webhook secrets belong in environment-specific secret store/injected files, not git. `NEXT_PUBLIC_*` values are public and must never contain secrets. Bootstrap admin through a one-time auditable command or controlled identity grant, enable MFA, then disable the bootstrap path. No magic hardcoded admin email.

## Local development sequence

1. Create ASP.NET solution, frontend workspace, worker and test projects.
2. Start PostGIS database and object storage with isolated local-only test credentials.
3. Generate EF Core migrations + ModelSnapshot; run migrations once; verify PostGIS extension/indexes/eligibility view.
4. Start API and Worker with fake email and fake Stripe adapter; run seeded fictional data.
5. Start Next.js frontend; verify `/en`, `/es`, public event and comedian route in browser.
6. Run API integration tests with real PostgreSQL/PostGIS; run React unit tests and Playwright.
7. Enable Stripe **test** credentials and forward signed test webhooks; verify exact 200/500 total, idempotency, deferred payment, refunds/disputes.
8. Rehearse backup/restore and emergency hide with cache invalidation.

## Staging gates

- Full end-to-end flows run from a clean test promoter and admin identity.
- HTTPS and SameSite/CSRF cookie behavior correct for actual deployed hostnames; no auth endpoints accessible cross-origin unintentionally.
- Map tiles loaded from approved license-compliant provider with rate/usage visibility; map still usable as list offline.
- Outbound email sender verified and unsubscribe/bounce process tested.
- Webhook endpoint registered with its own **staging signing secret**; no production Stripe keys in staging.
- Event/poster image upload scanning and retention verified; permission for publicity content controlled.
- Manual tax/legal confirmation still required before live charging.

## Production gate and operations

Only after signoff: provision DNS/TLS/domain, isolate secrets, register Stripe Live webhooks, verify Stripe tax-inclusive prices and matching tax code/registrations, publish legal notices, test support and refund process. Promote only tested immutable build artifacts. Deploy migration as a separate locked job after verified DB backup; deploy API/Web/worker; smoke test anonymous paid eligible listing, unpublished hidden item and comedian page. Observe webhook errors and reconciliation, worker outbox lag, moderation queue age, slow DB queries, map/tile errors and newsletter bounce/unsub rates.

**Alerts:** webhook failed 5+ min, stuck approved-awaiting-payment with paid Stripe order, mismatched price/currency, an unmoderated event unexpectedly public, emergency hide cache leak, worker down, database backup >24h, expired TLS certificate, health check failures. All alerts must redact secrets/PII.

**Daily:** backup/PostGIS integrity; **weekly:** review permissions, fraud, refunds, uptime and newsletter stats; **monthly:** restore drill (or more often in pilot) and Stripe/DB reconciliation. Keep actionable rollback docs. Do not automatically roll back payment migrations or erase paid order audit. Keep audit retained per legally approved policy.
