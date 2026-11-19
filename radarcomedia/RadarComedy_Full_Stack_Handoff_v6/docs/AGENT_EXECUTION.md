# Copy/paste implementation-agent kickoff prompt

You are the lead full-stack software engineer responsible for **building** the complete RadarComedy / RadarComedia MVP from the attached v6 handoff. This is an actual coding assignment, not a request to rewrite the spec.

## Inputs and precedence

Read `MASTER_SPEC.md` completely first, then `docs/BACKEND_AND_DATA.md`, `docs/FRONTEND_UX.md`, `contracts/openapi.yaml`, `docs/API_AND_FRONTEND_CONTRACTS.md`, `docs/TICKETS_AND_ACCEPTANCE.md`, and `docs/DEPLOYMENT_RUNBOOK.md`. The v6 master document has the highest authority. `reference/` is optional uncompiled previous work; do not treat it as production-ready code. Do not change locked business policies, pricing or moderation logic for convenience. For any nonessential ambiguity, select the safest explicitly documented default, record it in `DECISIONS.md`, and proceed. For tax/legal signoff and unclear refund rights, implement scaffolding but block production use rather than guessing.

## Build the actual system

1. Bootstrap one repo using a clear `apps/web` React/Next.js frontend, `apps/api` ASP.NET Core 10 API, `apps/worker` background service, `src/*` domain/application/infrastructure, migrations and tests.
2. Create **EF Core-generated** PostgreSQL/PostGIS migrations with checked-in ModelSnapshot, indexes, constraints, public eligibility view and real integration tests. Use a single migration authority.
3. Implement ASP.NET Core Identity secure cookie login, email verification and admin MFA, roles and resource-owner authorization.
4. Implement promoters, event snapshots, moderation, occurrence projection, fixed $2/$5 entitlements, recurring 3-calendar-month term, unlimited reviewed transfers, instant emergency hide, refunds/dispute safe state, comedian admin CRUD and public profile data, newsletter and strictly private API imports.
5. Implement exact OpenAPI contract (`contracts/openapi.yaml`) and generate TypeScript API types/client. Mark any deliberate breaking contract changes explicitly.
6. Build the **functional** public React map/list, event pages, comedian profile pages, promoter onboarding/dashboard/event wizard, checkout state, admin moderation, newsletter and EN/ES route UX; test real flows against API, not only mockups.
7. Use Stripe test mode initially, signed webhook verification, event dedupe and exact final tax-inclusive USD totals. **Never use Stripe success redirect as proof of publication**. Do not activate live payments before tax/legal signoff.
8. Add worker/outbox, weekly newsletter, profile/image safety, healthchecks, auth/rate limits, backups, CI and Docker Compose/Caddy deployment.
9. Test each P0 acceptance case from `docs/TICKETS_AND_ACCEPTANCE.md` using unit/integration/Playwright. Include timezone + DST fixtures, money checks, unpublished data leakage checks and repeated payment webhooks.
10. Return implementation source and instructions; do not pretend docs are the built app.

## Mandatory build sequence

Start with foundation + migrations + auth, then promoter and event moderation, recurrence and public eligibility, then Stripe, then map/list/event/comic pages and both portals, then newsletters and operational hardening. React pages may be parallelized once API DTOs exist, but integration must use generated contract. Commit and test incrementally; no giant unverified change.

## Report your work on each delivery

For each milestone provide:
- Tickets completed, in progress and blocked (IDs); references to commits/PRs.
- File tree changes and which services/routes are actually implemented.
- Commands executed and results (build, lint, DB migrations, unit/integration/E2E, CI, Stripe test webhooks).
- Explicit user-visible behavior verified for anonymous fan, promoter, admin and comedian profile.
- Security and tax/payment caveats; never invent deployment, test results or provider success.
- Next ordered tickets and owner decisions needed.

Keep `IMPLEMENTATION_STATUS.md` and `DECISIONS.md` updated. If a tool or credential is missing, isolate behind an interface/configuration and use a fake/test adapter; do not use production keys or promise external billing works.

## Final completion criteria

A nontechnical user can open a bilingual, mobile-friendly site and search public paid comedy performances on map/list, view event/comedian details, and open external ticket pages. A verified/admin-approved promoter can submit a show for approval, pay the exact $2/$5 fixed fee, and have that event become public only after verified payment. Admin can approve/reject/promoter and event edits, transfers and emergencies. A repeating production may have date-specific tickets but no more than one show per day and never outside the paid three calendar months. Comedian profile content is admin-managed in MVP and linked upcoming appearances are public-eligibility-filtered. Newsletter and full security/payment acceptance tests pass. The completed repo boots in staging with migrations and real health checks.
