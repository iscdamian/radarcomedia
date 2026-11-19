# RadarComedy / RadarComedia — Complete Developer Handoff v6

**Prepared:** November 18, 2026  
**Deliverable type:** implementation specifications, reference code and API contract. **This is not a built application.**

This package is designed to be passed directly to another coding agent (Codex, Claude Code, or another development app) or a human full-stack team. It includes the entire MVP product specification **from .NET backend/PostgreSQL to React/Next.js frontend**, plus build tickets, acceptance tests and an agent prompt.

## Start reading in this order

0. **`COMPLETE_SPEC_v6.md`** — if your coding app accepts just one file, give it this complete consolidated specification.
1. **`MASTER_SPEC.md`** — authoritative entire product, policies, functionality, architecture, domain, UX and release conditions. Start here.
2. **`docs/AGENT_EXECUTION.md`** — copy/paste kickoff prompt and requirements for the implementation agent.
3. `docs/BACKEND_AND_DATA.md` — .NET 10 architecture, EF Core/PostGIS mappings, migrations, background jobs, state transitions, Stripe, security.
4. `docs/FRONTEND_UX.md` — React/Next.js pages, components, UX flows, bilingual labels, map/list, promoter/admin dashboards, comedian profiles.
5. **`contracts/openapi.yaml`** — OpenAPI 3.0 contract for 48 planned API paths (identity, public, promoter, admin, payments).
6. `docs/API_AND_FRONTEND_CONTRACTS.md` — JSON examples, errors, auth rules and API typing.
7. `docs/TICKETS_AND_ACCEPTANCE.md` — ordered P0/P1/P2 tickets and 50 scenario-based acceptance tests.
8. `docs/DEPLOYMENT_RUNBOOK.md` — configuration, development/staging, observability, operations.
9. `reference/` — **pre-existing uncompiled C# entities/EF mapping and SQL designs** for context only; their migration path is not automatic. v6 master and actual EF-generated migrations take precedence.

## Product in 30 seconds

One bilingual mobile PWA discovers upcoming comedy in **San Diego / Tijuana** on a map and list. Anonymous fans browse and click external ticket links. **Only approved, paid promoter-owned listings** are public. Accounts require verified email + social/organizer URL + admin approval. A single event costs **$2 USD all-in**; a genuine recurring production costs **$5 USD all-in for three calendar months** and may have individually ticketed dates, at most one per day. A promoter can request **unlimited admin-approved date transfers** before the performance is completed. All event content changes need admin approval; emergency hiding is immediate and admin-reviewed later. Newsletter weekly by city+language. Comedians have free, **admin-managed** public profiles and paid-eligible appearance lists at launch; future verified profile claiming is a deferred phase.

## Baseline stack

- Next.js App Router + React + TypeScript strict, MapLibre (separate appropriate tile provider), PWA.
- ASP.NET Core .NET 10 modular monolith, ASP.NET Core Identity secure cookie login and admin MFA.
- EF Core + PostgreSQL 17/PostGIS/NetTopologySuite.
- Stripe Checkout one-time payments and signed webhooks.
- S3-compatible image store; .NET worker + outbox, transactional email + weekly newsletters.
- Docker Compose + Caddy, independent from other deployed apps.

## Quick handoff to an implementation agent

> Read all files in this ZIP. Treat `MASTER_SPEC.md` as authoritative. Implement the P0 backlog sequentially with tested, committed EF Core migrations, the OpenAPI contract, Next.js/React pages, all moderation and payment flows, and full acceptance suites. Begin with RC-001 through RC-008; use staging Stripe test mode and no real personal data. Do not claim a feature exists before code and tests confirm it; maintain `IMPLEMENTATION_STATUS.md` and report blockers. See `docs/AGENT_EXECUTION.md` for complete instructions.

## Known decisions requiring owner/legal confirmation

Refund/chargeback policy; exact Stripe tax code, obligations and registrations; approved domains/branding; official geographic coverage boundaries; initial moderation SLAs and legal/consent text. Build/test against safe configurable defaults, but **do not enable production payments before tax/legal confirmation**.

## Reference files warning

`reference/src/` contains earlier partial C# entities and mapping; `reference/db/V001-V003` are old SQL migration designs. They are included to accelerate implementation but have **not** been verified to compile together. In a new app, create a proper .NET solution and use **a single EF Core migration chain**, rather than applying reference SQL and EF migrations twice. Do not copy `reference` and claim the system is implemented.
