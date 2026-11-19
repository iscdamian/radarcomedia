# Engineering tickets, dependency order and testable acceptance criteria

**Sprint estimates are not calendar promises.** Tickets listed in dependency-first order; parallel tracks can run when their prerequisites are available. P0 all launch blockers, P1 pilot enhancements, P2 future. Refer to `BACKEND_AND_DATA.md`, `FRONTEND_UX.md` and `contracts/openapi.yaml` before implementing each item. This is the authoritative **v6 work plan**; `reference/BACKLOG_v5.md` is historical.

## A. Dependency-ordered P0 backlog

| ID | Work package | Dependencies | Concrete done criterion |
|---|---|---|---|
| RC-001 | Repository foundation + .NET 10/Next.js strict TypeScript | — | Builds API, Worker, Web, libraries; format/lint/test commands in CI; environment examples; no secrets |
| RC-002 | Docker Compose + Caddy staging | RC-001 | HTTPS reverse proxy, independently named project/networks/volumes/secrets, health checks; staging smoke test |
| RC-003 | PostgreSQL 17/PostGIS + EF Core migration system | RC-001 | EF-generated migration classes/snapshot; fresh DB test; correct geography point and GIST index; *one migration authority* |
| RC-004 | Domain model and database constraints | RC-003 | Promoter, event/revision, occurrence, entitlement/order, comedian, newsletter, audit with indexes and compile/test parity |
| RC-005 | Session auth / email verification / admin MFA | RC-001 | Login/logout/verify flows, HttpOnly cookies and CSRF protection; tested unauthorized requests |
| RC-006 | Authorization + owner scope | RC-004,RC-005 | Every promoter/admin endpoint enforces role+ownership; direct-ID attack tests pass |
| RC-007 | Promoter application | RC-005,RC-004 | Verified email+HTTPS organizer page required; PendingReview; no event submission until approval |
| RC-008 | Admin promoter review queue | RC-006,RC-007 | Approve/reject/suspend audited with reviewer, reason, timestamp; notifications delivered |
| RC-009 | Venues and geocoding | RC-004 | San Diego/Tijuana supported, IANA timezone and WGS84 coordinates, geospatial query tests |
| RC-010 | Event drafts and validation | RC-008,RC-009 | Approved promoters only, $2 or $5 plan selected, venue/schedule/tickets/lineup/poster validated; private draft |
| RC-011 | Immutable revisions | RC-010 | New changes create new revision; submitted snapshot immutable; stale version -> 409 |
| RC-012 | Admin event review and diff | RC-011,RC-006 | Review full snapshot, duplicate/identity rules; approve/reject; prior approved revision unchanged on reject |
| RC-013 | Recurrence engine | RC-010 | 3 calendar months, arbitrary future start, daily/weekly/monthly subset, <=1/day, DST fixtures pass |
| RC-014 | Occurrence projections/exception edits | RC-012,RC-013 | Atomic regeneration of approved projections; preserve past IDs/history; skip/move within term only |
| RC-015 | Paid entitlement model | RC-004,RC-013 | Single 200¢/Recurring 500¢; no auto renewal; no overlapping paid recurring terms |
| RC-016 | Stripe tax-inclusive Checkout | RC-012,RC-015 | Exact USD totals inclusive of tax; tax classification gated; session idempotency; no subscription object |
| RC-017 | Stripe signed webhook + durable inbox | RC-016 | Raw signature verification, 2+ replays idempotent, checks paid state/amount/order, transactionally active |
| RC-018 | Public eligibility projection | RC-014,RC-017,RC-008 | Single query excludes unpaid, pending, import-only, suppressed, canceled, out-of-term everywhere |
| RC-019 | Public discovery API | RC-018 | Map/list identical occurrence IDs, bounding/date caps, city/language/category/price filters, pagination |
| RC-020 | Web React app skeleton + EN/ES routes | RC-001 | `/en` `/es`, locale choice, Nav, city, page metadata, SSR/route structure, responsive base |
| RC-021 | Mobile map/list discovery | RC-019,RC-020 | MapLibre marker cluster + keyboard accessible list; denial of geolocation safe; same filter set |
| RC-022 | Event detail page + lineup | RC-019,RC-020 | SSR original description, venue-local schedule, approved performer links and safe external tickets; hidden -> 404 |
| RC-023 | Comedian profile administration | RC-004,RC-006,RC-012 | Admin create/edit/publish/hide free profile, unique slug, safe image/social links, audit; no promoter write |
| RC-024 | Public comedian profile and appearances | RC-018,RC-023,RC-020 | `/en/comedians/{slug}` and `/es`, metadata and eligible upcoming shows, empty state, hidden -> 404 |
| RC-025 | Promoter dashboard + create wizard | RC-010,RC-020 | 6 steps, draft persistence, schedule preview, validation, status derived from API |
| RC-026 | Admin React moderation screens | RC-008,RC-012,RC-020 | Promoter, event revision diff, transfer and comedian management, reason recorded, 409 conflict handled |
| RC-027 | Promoter Stripe UX | RC-016,RC-017,RC-025 | Pay only approved plan; pending on redirect; published after server-verified order only |
| RC-028 | Unlimited date transfers | RC-014,RC-015,RC-026 | 25 sequential same-show approved transfers with no repurchase, no duplicate date; completed barred |
| RC-029 | Emergency hide & admin restore | RC-018,RC-025,RC-026 | Immediate public suppression, audit + prioritized review; only admin can restore |
| RC-030 | Newsletter consent/data | RC-004,RC-020 | Double opt-in with token hash, city+language, unsubscribe, non-enumerating 202 |
| RC-031 | Weekly newsletter worker | RC-018,RC-030 | Only future eligible shows, localized template, idempotent send, bounce/suppression |
| RC-032 | Outbox, expiry & payment reconciliation | RC-017,RC-015 | Durable job retries, webhooks missing/late reconciled, no duplicate publish, far-future term stays valid |
| RC-033 | Image upload and abuse controls | RC-010,RC-023 | Pre-signed upload, limits/scanning/EXIF strip, rights checkbox, rate limits and event reporting |
| RC-034 | SEO, PWA, a11y, performance | RC-021,RC-022,RC-024 | Real SSR content, eligible sitemap, hreflang, metadata, map fallback, mobile/a11y tests |
| RC-035 | Complete integration + E2E suites | RC-017 through RC-034 | Public eligibility, payments, recurrence, admin, profile, transfer and newsletter test matrix passes |
| RC-036 | Observability and backup restore | RC-002,RC-032 | Health/alerts, backup, off-host restore drill, secret/env separation, real staging runbook |
| RC-037 | Tax/legal/policy launch gate | RC-016,RC-035 | Qualified pricing/tax review, refund/chargeback policy, privacy, terms, image/import permissions, SLA |
| RC-038 | Pilot operations | RC-037 | 10 legitimate approved promoters and 30 actual paid eligible performances target; support/quality monitoring |

## B. P1 and P2 work

| ID | Priority | Description | Done criterion |
|---|---|---|---|
| RC-039 | P1 | Permitted public API source discovery adapter | Terms reviewed; source register; internal import candidate only; never public automatically |
| RC-040 | P1 | Promoter claim candidate workflow | Candidate can be claimed only by approved organizer and ordinary review/payment |
| RC-041 | P1 | Minimal promoter conversion analytics | Safe aggregate views/clicks, no private fan tracing; promoter dashboard |
| RC-042 | P1 | Content editorial and SEO city pages | No unpaid/unapproved listing leaks; meaningful original content |
| RC-043 | P1 | Venue directory and show calendars | Only eligible paid show dates shown; venue permission checks |
| RC-044 | P2 | Optional fan accounts and favorites | Anonymous browsing remains available; privacy documented |
| RC-045 | P2 | Verified comedian claim + self-service drafts | Explicit identity proof, admin approval, ownership conflict handling, versioned edits |
| RC-046 | P2 | AdSense | Only after approval and enough original content; no map/control obstruction |
| RC-047 | P2 | Geographic expansion | Configurable new city boundaries, zones, legal/tax assessment |
| RC-048 | P2 | Native apps | Demand validated and same API reused |

## C. Functional acceptance matrix

| AC | Given / when / then |
|---|---|
| AC-001 | Anonymous phone visitor opens `/en` or `/es` → sees map/list without login or mandatory geolocation. |
| AC-002 | Same filters in map/list → same eligible occurrence IDs and counts (subject to same page/cursor). |
| AC-003 | Pending/suspended promoter has draft or imported candidate → zero public appearance in any API/search/sitemap/newsletter. |
| AC-004 | Admin approved event without paid entitlement → still nonpublic. |
| AC-005 | Verified paid event + active approved owner + approved revision + scheduled occurrence → public. |
| AC-006 | Admin suspends promoter, hides event, or refund revokes entitlement → event disappears immediately from live API. |
| AC-007 | Promoter submits change to published show → public version remains unchanged during review. |
| AC-008 | Rejected or stale revision → public snapshot unchanged; 409 on stale decision. |
| AC-009 | Email verified and social/organizer URL supplied, but account pending → cannot create/submit event via direct API. |
| AC-010 | Promoter approved by admin with reviewer/time/reason → may create private draft. |
| AC-011 | Single event checkout → final total exactly 200 USD cents inclusive of tax. |
| AC-012 | Recurring checkout → final total exactly 500 USD cents inclusive of tax. |
| AC-013 | Stripe checkout or tax config exceeds fixed total → fail closed before publication. |
| AC-014 | User returns to success URL without webhook payment proof → status still unpublished/pending. |
| AC-015 | Same signed Stripe event delivered 10 times → one paid order and one entitlement; no double publish. |
| AC-016 | Payment completed after async delay → eventual verified activation only once. |
| AC-017 | Same recurring Thursday production uses different ticket URLs weekly → valid one $5 series. |
| AC-018 | Different separately ticketed headline festival productions → cannot be combined under one recurring entitlement. |
| AC-019 | 2027-01-10 recurring start → term endExclusive 2027-04-10. |
| AC-020 | Recurring date moved to exact endExclusive → rejected. |
| AC-021 | A show begins years after payment → allowed; calendar anchor unchanged; paid entitlements not prematurely expired. |
| AC-022 | Two scheduled recurring occurrences on same local day → rejected even if local times differ. |
| AC-023 | DST spring/fall changes → correct venue-local scheduled wall time and nonzero UTC duration. |
| AC-024 | One-time paid event moved 25 times before happening, all admin approved → remains one entitled, one visible upcoming performance. |
| AC-025 | Completed one-time event transfer to new show/date → rejected, even if count limit is unlimited. |
| AC-026 | Unrelated production/title and changed ownership transfer attempt → rejected or escalated to new paid listing. |
| AC-027 | Promoter emergency hides occurrence → disappears from public map/list/profile/newsletter immediately; admin queue item created. |
| AC-028 | Promoter attempts to self-restore hidden occurrence → forbidden. |
| AC-029 | Admin restores falsely canceled show with reason → eligible only if paid/approved and future. |
| AC-030 | Free public comedian profile in Published state → page indexed and loads with native bio and socials. |
| AC-031 | Draft/Hidden comedian page via guessed URL → 404, not leaked through search. |
| AC-032 | Published comedian appearing in unpaid or hidden show → that appearance absent on their page. |
| AC-033 | Comedian has zero upcoming approved appearances → profile still loads with friendly empty state. |
| AC-034 | Promoter guesses comedian write endpoint/ID → 403; admin alone manages bio/photo/social in MVP. |
| AC-035 | Comedian claim edit attempt in MVP → no enabled endpoint; future feature remains deferred. |
| AC-036 | Switch English↔Spanish UI → labels translated; original event description and original comedian bio unchanged. |
| AC-037 | San Diego visitor filters Spanish-language Tijuana show → show language and locale are independent. |
| AC-038 | Ticket price MXN displayed correctly, platform fee remains USD. |
| AC-039 | Geolocation denied or map tile provider fails → accessible list still works. |
| AC-040 | Public sitemap/structured data includes only truly eligible approved, paid events. |
| AC-041 | Anonymous newsletter signup → explicit consent, confirmation email, then weekly selection by city/UI language. |
| AC-042 | Unsubscribed or bounced email → never sends again without new opt-in. |
| AC-043 | Newsletter generated after emergency hide → hidden show excluded. |
| AC-044 | Imported API candidate without promoter payment → never listed anywhere public. |
| AC-045 | Media upload malicious/oversized/wrong MIME → rejected; not publicly accessible. |
| AC-046 | Another promoter's event ID is used in request → 403/404 owner scope enforced. |
| AC-047 | Signed webhook replay after worker restart → no duplicate emails/entitlements. |
| AC-048 | DB backup and restore with paid, hidden, revisions data → visibility and audit invariants preserved. |
| AC-049 | All admin decisions contain reviewer ID, time, reason and subject in audit. |
| AC-050 | Browser navigates mobile 320px–1440px, screen reader/list and keyboard paths work without unusable overflow. |

## D. Automated test organization

```text
tests/unit/             # DateOnly.AddMonths, DST, plan, auth decisions, transfer policy
 tests/integration/      # PostGIS DB + real migration, public eligibility query, unique indexes
 tests/contracts/        # OpenAPI request/response and errors, TS generation drift
 tests/e2e/              # Playwright mobile+desktop with seeded fictional data
 tests/payments/         # mocked Stripe signed webhook replay + separate staging Stripe test mode
 tests/security/         # RBAC matrix, cookie/CSRF, overposting, SSRF/open-redirect, upload
 tests/operations/       # restore drill, worker retry, stale cache/effect of emergency hide
```

Required fixture seeds: paid single, approved unpaid single, pending promoter event, recurring show with 13 weekly ticket URLs, artist with public/private appearances, artist with zero future shows, imported private candidate, emergency hide, failed/refunded payment, first recurring show years in future, leap-day and DST edge shows.

## E. Definition of done / handoff completion report

Every ticket: PR/commit, tests, OpenAPI/TS client compatibility, security/ownership check, visible/error/empty state, telemetry/alerts where relevant, docs updated, local+staging verification. Another coding agent must return a checklist of **implemented vs not implemented**, commands executed with outputs, migration names, API spec diff, screenshots/test output links, staging URLs if actually deployed, blockers/assumptions and next tickets. Never mark completed solely because a file/spec exists.
