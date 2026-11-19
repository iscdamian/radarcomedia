# Engineering backlog — RadarComedy v5

**Planning convention:** P0 = blocking first launch, P1 = launch hardening/growth, P2 = post-launch. Story points are rough relative sizing, not promised calendar dates. The `Depends` column is a dependency, not a dependency graph guarantee. Each ticket must have code review, test coverage, telemetry, and updated docs before closure.

## Foundation (P0)

| ID | Pri | SP | Depends | Engineering outcome / acceptance |
|---|---|---:|---|---|
| RC-001 | P0 | 3 | — | Monorepo/service structure for Next.js + ASP.NET Core 10 + worker, CI build and test. No secrets committed. |
| RC-002 | P0 | 5 | RC-001 | Compose/Caddy for independent app, health checks, TLS staging domain, environment separation, DB backup plan. |
| RC-003 | P0 | 5 | RC-001 | PostgreSQL/PostGIS setup, SQL V001/V002 applied in migration runner with version history and rollback/restore plan. |
| RC-004 | P0 | 5 | RC-003 | EF Core entity mapping + repository layer, constraints, concurrency markers, stable UUID IDs. Model-vs-SQL parity test. |
| RC-005 | P0 | 5 | RC-001 | Auth, email verification, secure HttpOnly cookies, anti-CSRF, session renewal, admin MFA. |
| RC-006 | P0 | 3 | RC-005 | Role/ownership policies; denied attempts for every promoter/admin route with 403 and audit. |
| RC-007 | P0 | 3 | RC-005,RC-004 | Promoter application requires verified email + HTTPS social/organizer URL, city, contact. Status initially PendingReview. |
| RC-008 | P0 | 3 | RC-006,RC-007 | Admin promoter approval/reject/suspend, actor/time/reason and notifications. Pending cannot submit or pay. |

## Catalog and discovery (P0)

| ID | Pri | SP | Depends | Engineering outcome / acceptance |
|---|---|---:|---|---|
| RC-009 | P0 | 5 | RC-004 | Venue CRUD and geo validation; accurate San Diego/Tijuana locality + IANA zone; lon/lat and PostGIS indexed query. |
| RC-010 | P0 | 5 | RC-008,RC-009 | Promoter event draft wizard: one-time vs recurring, category, show language, lineup, description, admission/ticket link. |
| RC-011 | P0 | 5 | RC-010 | Immutable revision snapshots, compare/diff and optimistic-concurrency validation. Submitted edits never mutate live rows. |
| RC-012 | P0 | 5 | RC-011,RC-006 | Admin event moderation UI and approval workflow. Approval creates safe published projections, but not public unless paid. |
| RC-013 | P0 | 5 | RC-012,RC-004 | Public eligibility view/query and tests: no unpaid, imported, hidden or unapproved leaks. Map/list same ID results. |
| RC-014 | P0 | 5 | RC-013 | Public occurrence API: city/bbox/date filters, paging, max range and query budget; stable error codes. |
| RC-015 | P0 | 5 | RC-014 | Mobile-first MapLibre map, marker clusters, accessible list fallback, city/date/category/show-language filters. |
| RC-016 | P0 | 3 | RC-014 | Event/occurrence detail, correct ticket redirect validation and share links, local timezone/lineup. |
| RC-017 | P0 | 3 | RC-015,RC-016 | EN/ES UI, locale routing/hreflang/SEO; do not auto-translate original promoter content. |
| RC-043 | P0 | 5 | RC-004,RC-006,RC-012 | Admin comedian CRUD, unique slugs, moderated publication/hide, legal image/social URL review, V003 fields and audit. Promoters can suggest names but cannot edit profiles. |
| RC-044 | P0 | 5 | RC-043,RC-013,RC-016 | Public `/en/comedians/{slug}` and `/es/comedians/{slug}`, public API detail/upcoming appearances, links from event lineups, no unapproved/unpaid show leaks, safe empty state. |

## Recurrence, revisions and safety (P0)

| ID | Pri | SP | Depends | Engineering outcome / acceptance |
|---|---|---:|---|---|
| RC-018 | P0 | 8 | RC-010 | Recurrence engine: daily/weekly/monthly subset, max 1/day, 3-month DateOnly.AddMonths(3), far-future first date allowed, DST fixtures. |
| RC-019 | P0 | 5 | RC-018,RC-011,RC-012 | Publish approved revision and safely regenerate occurrences (atomic replace/upsert; preserve historical identifiers as applicable). |
| RC-020 | P0 | 5 | RC-019 | Unlimited single date-transfer requests, admin approval and audit, same-production + incomplete-only invariants. |
| RC-021 | P0 | 5 | RC-018,RC-019 | Recurring occurrence skip/move exceptions via approved revision; never outside paid term or >1/day. |
| RC-022 | P0 | 3 | RC-013,RC-006 | Emergency hide endpoint immediately removes occurrence from public view, queues admin review; restore admin-only. |
| RC-023 | P0 | 3 | RC-012,RC-019 | Every promoter modification requires approval. Stale moderation decision conflicts without public overwrite. |

## Payments and entitlements (P0)

| ID | Pri | SP | Depends | Engineering outcome / acceptance |
|---|---|---:|---|---|
| RC-024 | P0 | 5 | RC-018,RC-012,RC-004 | Listing entitlement machine; separate Single=$2 and RecurringThreeMonths=$5; no Stripe subscriptions. |
| RC-025 | P0 | 5 | RC-024 | Stripe Checkout creates gross 200/500 USD cents with inclusive tax behavior, eligible approved listing only; tax setup gated. |
| RC-026 | P0 | 8 | RC-025 | Raw signed Stripe webhooks and durable inbox; only verified settled payment activates entitlement; duplicate replay safe. |
| RC-027 | P0 | 5 | RC-026,RC-013 | Public release depends on verified Active entitlement, admin approval, valid occurrence and no emergency hide. |
| RC-028 | P0 | 5 | RC-026 | Refund/chargeback/payment reconciliation and alerting; duplicate Stripe sessions identified and resolved, no free public listing. |
| RC-029 | P0 | 3 | RC-024,RC-027 | Scheduled expiry/renewal worker, no auto-billing; repeat paid terms modeled separately. |

## Messaging, imported data, launch hardening (P0/P1)

| ID | Pri | SP | Depends | Engineering outcome / acceptance |
|---|---|---:|---|---|
| RC-030 | P0 | 5 | RC-013,RC-017 | Newsletter signup (double opt-in), city/language, weekly content eligibility, unsubscribe and bounce handling. |
| RC-031 | P0 | 3 | RC-006,RC-013 | Anti-spam, rate limits, event reporting, safe poster uploads (size/MIME/EXIF and scanning). |
| RC-032 | P0 | 8 | RC-026,RC-027,RC-023 | xUnit/API/Playwright E2E: paid/unpaid visibility, admin review, transfer 10+ times, DST, far-future, checkout replay. |
| RC-033 | P0 | 5 | RC-002,RC-032 | Alerts and logs for payment failures, moderation backlog, worker errors, daily backups and restore drill. |
| RC-034 | P0 | 3 | RC-028,RC-030 | Prelaunch signoff: published refund policy, tax evaluation, terms, privacy, newsletter compliance and supported cities. |
| RC-035 | P1 | 5 | RC-013 | Permitted API import adapters with source-license register and *private* candidate staging (no public feeds). |
| RC-036 | P1 | 3 | RC-035,RC-012 | Promoter claim workflow requires ordinary approval and paid listing, never auto-publication. |
| RC-037 | P1 | 5 | RC-027 | Promoter analytics (detail views, outbound ticket clicks) aggregated without invasive tracking. |
| RC-038 | P1 | 3 | RC-030,RC-027 | Weekly organizer/event spotlights, deliverability dashboard, newsletter reporting. |
| RC-039 | P1 | 3 | RC-033 | SEO, performance budgets, CDN caching with immediate invalidation for emergency hides. |
| RC-040 | P2 | 5 | RC-039 | Optional fan accounts/favorites and followed-comedian reminders. |
| RC-041 | P2 | 3 | RC-039 | Apply for AdSense if eligibility satisfied; no map overlays or accidental-click placements. |
| RC-042 | P2 | 8 | RC-037 | Geographic expansion and native app evaluation, based on verified audience demand. |
| RC-045 | P2 | 5 | RC-043 | Verified comedian claim/self-management workflow, artist removal requests and ownership transfer with moderation. |

## Critical path

`RC-001 → RC-003/004 + RC-005/006 → RC-007/008 → RC-009/010 → RC-011/012 → RC-018/019 → RC-024/025/026/027 → RC-013/014/015/016/017 + RC-043/044 → RC-030/032/033/034`.

Public browsing and promoter UI can be implemented in parallel after foundation, but production release is blocked by verified-payment eligibility and moderation. Run database migration constraints and payment replay tests **before** inviting paying promoters.

## Three workstreams, if multiple developers available

- **Backend/DB:** RC-003,004,006–014,018–029.
- **Frontend/UX:** RC-010–012 UI, 015–017, admin portal and checkout UX.
- **Infrastructure/quality:** RC-001,002,005,030–034, CI/integration tests.

## Suggested pilot gates (not demand predictions)

- At least 10 approved promoters and 30 upcoming **paid** eligible performances across both markets before broad fan marketing.
- 100% of publicly returned results match paid+approved eligibility checks in automated tests.
- An admin can approve a promoter, approve an event, observe verified payment, and see the event on map from a clean browser in one E2E scenario.
- A single $2 listing can transfer multiple times under admin review with no repurchase and no duplicate active dates.
- A 3-month series preserves wall time across DST and does not display out-of-term shows.
- Zero unreviewed changes visible to the public, except immediate safety **hiding** of possibly canceled content.
