# RadarComedy / RadarComedia — Single-File Complete Engineering Specification v6

**Prepared November 18, 2026.** This document combines the authoritative master spec and all implementation guides into one file to paste into a coding agent. The ZIP additionally includes machine-readable OpenAPI, legacy model references, and checklists. **This is a specification, not implemented code.**



---

## PART 1: MASTER_SPEC.md

# RadarComedy / RadarComedia — Complete Full-Stack Product & Engineering Specification

**Version:** 6.0 · **Date:** 2026-11-18 · **Audience:** coding agent / senior engineering team · **Status:** authoritative implementation specification, not implemented code.

> **One-paragraph build instruction:** Build a bilingual (English/Spanish), mobile-first, SEO-friendly Progressive Web App where anyone can discover upcoming **paid-and-approved** comedy events in **San Diego, California and Tijuana, Baja California** on a map and list. Approved promoters create single-event ($2 USD) or recurring-production ($5 USD/three calendar months) listings, obtain manual event review, and pay with Stripe before going public. Admins approve promoter identities, all event edits, and transfers. Fans can browse without signing in, link out for tickets, and subscribe to a weekly city/language newsletter. Comedians have **free, admin-managed public profile pages** with automatic upcoming eligible appearances. Everything is implemented as a modular .NET 10 backend + Next.js/React TypeScript frontend + PostgreSQL/PostGIS + worker, deployed with Docker Compose/Caddy. A future phase allows verified comedians to claim/manage their profiles; **do not implement self-service comedian editing in MVP**.

## 0. Document authority, build discipline and scope

This `MASTER_SPEC.md` is the single source of truth. Companion files in `docs/` elaborate on backend/frontend architecture, UX, testing, API and tickets. `contracts/openapi.yaml` is the contract to implement and extend; generate TS client/types from it. `reference/` contains preexisting **noncompiled reference C# models and SQL scripts**, not an independently validated app or migration history. Where a reference artifact disagrees with this v6 spec, follow this document, update the contract, and add a test. Never silently reinterpret pricing, publication eligibility, approval requirements, or profiles.

**Required deliverable from coding agent:** working source repository; EF Core migrations with snapshot; seed scripts for **fictional dev/test data only**; real API implementation; production-quality React UI; worker; tests; environment examples; staging deployment instructions; operational runbook; and an explicit blocked-issues ledger. Do not claim payment, tax, permission, or external API integration works until verified end-to-end. Do not commit real credentials.

**MVP in scope:** public map/list and event details; comedian details; venue basics; EN/ES UI; promoter registration/email verification/approval; event form, poster, recurrence, lineup, pending revisions; admin moderation; one-time and recurring paid listing entitlements; Stripe checkout/webhooks; emergency hiding/cancellation; transfers; newsletter; minimal SEO; accessibility; audit, monitoring and backups.

**Out of scope:** native mobile apps; selling tickets; promoter subscriptions; free public imported events; fan-required accounts; comedian self-service; reviews/comments; in-app social feed; AI recommendations; automatic translation of show descriptions; AdSense requirement for launch; sophisticated ML ranking.

## 1. Product goals, people and success

**Jobs to be done**
- Fan: “Open a map, choose city or optionally share approximate location, filter by date/language/genre/price, see reliable upcoming comedy shows, learn who performs, go to ticket provider.”
- Promoter: “After approval, list and keep one production accurate at an affordable flat amount, pay safely, see status, and request changes without losing a live listing.”
- Admin: “Approve legitimate organizers and shows, catch fraud/duplicates, review diffs/transfers and respond rapidly to cancellations and billing exceptions.”
- Comedian: “Have a discoverable profile and show upcoming verified appearances without having to maintain an account during MVP.”

**Audience:** English- and Spanish-speaking users in San Diego and Tijuana; local promoters, comedy clubs, venues, improv and sketch groups, festivals and theaters. Live comedy categories: `StandUp`, `OpenMic`, `Improv`, `Sketch`, `ComedyTheater`, `Festival`, `OtherLiveComedy`. Mixed-language productions allowed.

**Pilot goals (targets, not forecasts):** 10 approved promoters and 30 upcoming *paid eligible* performances across two markets before a major fan marketing push; 100% of results satisfy publication eligibility; 0 visible unapproved edits; 0 duplicate listing activations across webhook replays; one clean PWA and complete a promoter paid listing end-to-end. Track event inventory by city, active promoters, paid conversions, public event views, outbound ticket-link clicks (privacy-friendly), newsletter opt-ins, unsubscribe rate, moderation latency and canceled-show correction latency.

## 2. Locked product policy — nonnegotiable for MVP

| Policy | Implementation requirement |
|---|---|
| Single event | **$2.00 USD total**, inclusive of applicable tax, buys publication of one not-yet-completed performance, no renewal |
| Recurring | **$5.00 USD total**, inclusive of applicable tax, buys one genuine recurring production for **three calendar months**; no auto-renewal |
| Recurrence identity | Same recurring production, max **one performance per local calendar day**, even if each date has its own ticket checkout |
| Recurrence start | First approved scheduled local performance date; **no advance booking limit** |
| Recurrence end | Exclusive `firstLocalDate.AddMonths(3)` in the approved venue's IANA time zone; cannot move an occurrence outside this period |
| Other individually ticketed productions | Charge **$2 per distinct performance**; unrelated theater/festival programs do not count as one series |
| Promoter | Registered email-verified account with a real HTTPS social/organizer page, **manually approved by admin** before posting |
| Event review | Initial submission and **every proposed content edit** require admin approval; published revision remains visible while edit awaits review |
| Payment timing | Admin approves event content **before** promoter pays; do not make event public before confirmed, reconciled payment |
| Date transfer | **Unlimited admin-approved transfers** for the same not-yet-completed one-time production, no extra listing fee; cannot reuse finished event |
| Canceled event | Promoter may instantly **emergency hide** an occurrence; creates admin review/audit item; promoter may not restore or silently edit publicly |
| Published inventory | **Only approved, paid promoter-created/claimed listings** appear publicly. Imported API candidates remain strictly private until promoter claims, reviews and pays |
| Ticket sales | External HTTPS links only, no commission or in-app ticketing |
| Fan account | Not required to browse; optional favorites/accounts deferred |
| Newsletter | Weekly; segmented by chosen city and preferred UI language; explicit consent/double opt-in, one-click/easy unsubscribe |
| Profiles | Published comedian detail pages are **free**; created and maintained by admins in MVP; verified self-management future phase |
| Translation | Localize UI, emails, notices and labels. Preserve event title/description/comedian bio as submitted or admin-authored; do not machine-translate automatically |
| Platform | Single product, bilingual routes and optional Spanish vanity domain; responsive PWA before native apps |

**Temporal policy choices that must be implemented explicitly:** A paid recurring entitlement's start anchor is frozen at first approved scheduled date on activation; if that initial occurrence is later canceled, do *not* silently roll forward and extend the three months. Date-only arithmetic uses venue time zone, not UTC; `2027-01-10` through `2027-04-09` covers a 3-month term (end exclusive April 10). For monthly starts on 29–31 use `DateOnly.AddMonths(3)` calendar clipping; add tests. Use explicit DST rule: ambiguous fall-back hour selects the earlier offset; nonexistent spring-forward hour shifts forward to first valid local instant, warn organizers on scheduling; test each city zone. Revalidate time zones and use IANA IDs (`America/Los_Angeles`, `America/Tijuana`) rather than assuming they always share offset.

**Unresolved policy sign-offs, NOT developer choices:** refund/chargeback consumer rights; specific tax/product-service category and registration, including US/Mexico cross-border treatment; terms and privacy notice; whether venue-change within same production is allowed beyond one-time transfer; moderation SLA; support contact; domain/trademark clearance; legal rights to imported API content. Default unsafe/unknown cases to pending admin review or disabled, not automatically approved. These do not block local/staging implementation with fake payments.

## 3. System architecture and module boundaries

**Stack baseline:**
- **Frontend:** React + TypeScript via **Next.js App Router**, TypeScript strict, SSR/SSG where appropriate for SEO, client-rendered MapLibre map, accessible list fallback, CSS modules or Tailwind + accessible primitives. Install PWA manifest/service worker carefully; do not cache eligibility indefinitely.
- **API:** ASP.NET Core **.NET 10**, modular monolith; EF Core/Npgsql and NetTopologySuite; policy-based auth; RFC 9457 Problem Details; Swagger/OpenAPI; UTC instants, `DateOnly`/`TimeOnly` for venue-local planning.
- **Data:** PostgreSQL 17 + PostGIS; single source of truth; optimistic concurrency and transactional entitlement/visibility updates; object storage (S3-compatible/MinIO) for artwork with safe image processing.
- **Workers:** .NET worker service using DB-based outbox + scheduler for newsletters, recurrence materialization/horizon, entitlement expiration, Stripe reconciliation, reminders and image processing. Jobs idempotent.
- **Payments:** US Stripe account, hosted Checkout in one-time `mode=payment`; server-generated fixed tax-inclusive Price amounts; webhook as settlement authority.
- **Email:** transaction provider for verification, approvals, receipts/notifications and newsletter; separate templates, suppression/bounce management; do not use blocked password-based SMTP.
- **Deployment:** separate Compose project with Caddy HTTPS reverse proxy, independent DB/user/secrets/volumes/backup from Santa Llama. CDN only with immediate purge or very short cache for public eligibility.

```text
                         browser / PWA
                       Next.js (EN / ES)
                      /              \
          MapLibre client            SSR public SEO pages
                      \              /
                    HTTPS /api/v1 (same origin)
                             |
                   ASP.NET Core API
             Auth | Catalog | Moderation | Billing
             Public | Comedians | Newsletter | Admin
                  /          |           \
          PostgreSQL      Object store    Stripe Checkout
          + PostGIS           media        + signed webhooks
               |
          .NET Worker -> outbox/jobs, email, expiry, reconciliation
```

**Recommended repo layout**
```text
radarcomedy/
  apps/web/                  # Next.js App Router, React TS
  apps/api/                  # ASP.NET Core HTTP host + auth
  apps/worker/               # .NET background jobs
  packages/contracts/        # generated TS API client / enums
  src/RadarComedy.Domain/    # entities, domain rules, results
  src/RadarComedy.Application/ # commands/queries, ports, handlers
  src/RadarComedy.Infrastructure/ # EF Core, Stripe, object storage, email
  tests/unit/  tests/integration/  tests/e2e/
  infra/compose/ infra/caddy/ infra/scripts/
  docs/ contracts/ db/
```

**Auth:** Use ASP.NET Core Identity with secure first-party cookie authentication for MVP. Email verification required. Use secure HttpOnly session cookies, CSRF protection on state changes, secure same-site strategy on shared-origin frontend/API, session rotation/revocation, admin TOTP MFA, rate limits and verified role/owner checks on server. Do not expose Stripe secret, admin tokens, connection strings, email API credentials or service account keys to `NEXT_PUBLIC_*` variables.

## 4. Domain and database model

**Core aggregates (recommended public names; use actual C# names from reference if consistent):**

| Aggregate/entity | Required responsibility and fields |
|---|---|
| `AppUser` | Id, emailNormalized, emailVerifiedAt, role, createdAt, disabledAt, auth security reference |
| `Promoter` | Id, ownerUserId, name, contact, social/organizer URL, city, status, reviewer, timestamps, version |
| `Venue` | Id, name/address/country/city, IANA zone, geocoded `geography(Point,4326)`, approved status |
| `ComedyEvent` | Id, promoterId, slug, category, `Single/Recurring`, publishedRevisionId, suppression state, version |
| `EventRevision` | Immutable snapshot JSON, revision number, basedOn, submittedBy, moderation status/reviewer/reason; snapshots include title, description, lineup, posters, venue, dates, admission and ticket URLs |
| `EventOccurrence` | Id, eventId, revisionId, venueId, localDate/time, `startsAtUtc/endsAtUtc`, ticket URL, admission price/currency, status, hiddenAt |
| `RecurrenceSchedule` | EventId, frequency/interval/day mask, venue time zone, local start/duration, published revision |
| `OccurrenceException` | Approved skip/move, original local date and replacement date within term |
| `ListingEntitlement` | Id, eventId, plan, grossCents, USD, pending/active/expired/revoked state; singleOccurrenceId OR recurring start/end dates |
| `CheckoutOrder` | Id, entitlementId, grossCents, inclusiveTaxCents, Stripe session/intent, payment state, timestamps |
| `StripeWebhookInbox` | Unique event id, event kind, received/process status, payload checksum/pointer, retries; dedupe key |
| `TransferRequest` | Entitlement/occurrence, proposed revision, new date, reason, reviewer, decision, audit; unlimited requests permitted, not simultaneously active for same occurrence |
| `EmergencyHide` | Occurrence, reporter, reason, hiddenAt, review state, resolver, restoration audit |
| `Comedian` | Id, unique slug/stageName, native bio, photoKey, home city/country, languages, verified social links, `Draft/Published/Hidden`, admin approval fields |
| `EventPerformer` | ComedianId + occurrenceId, billing order, optional approved display override/role |
| `NewsletterSubscriber` | Email, city, language, confirmation + consent dates, token hashes, unsubscribe/suppression |
| `ImportSource/ImportCandidate` | Internal-only permitted source record, terms metadata, candidate fields, never public by itself |
| `ModerationAction/AuditLog` | Actor, action, subject, before/after references, time, reason, request correlation |
| `OutboxMessage/JobLease` | Idempotent email, payment and indexing jobs and leasing |
| `EventReport` | User reports of duplicate/canceled/fraudulent event; moderation resolution |

**DB rules and indexes:** uuid PKs; unique normalized user email; unique event/comic slug; unique (eventId, revisionNumber); partial unique one scheduled occurrence per event/localDate; `GEOGRAPHY(Point,4326)` GIST index; event/search `(city, startsAtUtc, status)` indexing via `Venue` and public projection; unique webhook event id, checkout session id, and paid order per entitlement; foreign keys; positive prices and durations; recurring entitlement non-overlap constraint; check exact plan amounts; audit columns and row-version optimistic concurrency. Validate venue and occurrence ownership server-side.

**Public projection:** central `PublicOccurrenceQuery` / SQL view `public_eligible_occurrences` handles *all* filters: approved and active promoter, approved published revision, verified paid active entitlement, scheduled not hidden or suppressed occurrence, event within purchased term/single occurrence match, country/city allowed, valid upcoming criteria; no public imported candidates. Do **not** duplicate this eligibility predicate across map/list, profiles, sitemap or newsletters. For `Comedian` appearances also require `Comedian.ProfileStatus=Published` and join `EventPerformer` through eligible occurrences. Profile page may show no appearances without being removed from public.

**EF Core migrations:** Prefer `dotnet ef migrations add InitialCreate` and subsequent migrations with checked-in `ModelSnapshot`, using one migration authority. Run SQL constraints/partial indexes/views via audited `migrationBuilder.Sql(...)` where Fluent API cannot express them. Do **not** apply the reference raw SQL *and* EF migration chain on the same database. Full migration sequence and example commands in `docs/BACKEND_AND_DATA.md`.

## 5. State machines and atomic actions

**Promoter:** `PendingReview` → `Approved` / `Rejected`; `Approved` → `Suspended`. Verify email and URL evidence before approval; preserve reviewer/time/reason. Suspended promoter's public listings suppressed until reviewed; do not implicitly refund.

**Revision:** `Draft` → `Submitted` → `Approved` / `Rejected` / `ChangesRequested`; a newer approved revision becomes `PublishedRevisionId` atomically. Mutating approved snapshot forbidden. Submitting a new edit after approval creates another revision, never an in-place edit.

**Entitlement/order:** `PendingPayment` → `Active` only after verified paid checkout; `Active` → `Expired`/`Suspended`/`Revoked`. `CheckoutOrder`: Created → CheckoutOpen → Paid/Failed/Canceled/Refunded/Disputed. Refund/dispute events trigger entitlement reconciliation and, where appropriate, suppression. Never trust return URL.

**Single event transfers:** any number of review-approved transfers permitted before one performance is completed. Transfer always refers to same event/entitlement; reviewer verifies *same production* and not already completed. Replacing one scheduled occurrence must be transactional and not create a second public show. A promoter cannot move one fully completed listing to advertise another future event for free. Drafted transfer leaves original listing publicly unchanged until approved. For cancel-then-transfer, emergency hide suppresses original while review awaits.

**Recurring series:** first approved date sets 3-calendar-month term at activation. Each occurrence may have its own ticket URL and lineup. Maximum one scheduled performance per local day. Approved reschedules/skips must lie within exclusive bounds and preserve production identity; never broaden term via edits or cancellation. Renewal is new explicit checkout/entitlement with no automatic billing; don't overlap paid terms.

**Emergency hide:** authenticated owning promoter calls `POST /promoter/occurrences/{id}/emergency-hide` with reason; write immediate `emergency_hidden_at`, append audit/review task, invalidate caches, 202/204. Admin confirms or dismisses/restores; promoter cannot self-restore. Hiding is a suppression action, not an approved textual content edit, so it is the sole immediate safety exception to every-edit review.

## 6. Personas, navigation, and screens

**Route convention:** `/en/...` and `/es/...`; root selects locale preference or English default, explicit switcher retained; shared identical event/comic slugs regardless of UI language. Suggested home route `/en` `/es`; `/[locale]/events/[slug]`, `/[locale]/comedians/[slug]`, `/[locale]/venues/[slug]` (venue index/details P1 if schedule permits), `/[locale]/newsletter`, `/[locale]/legal/{privacy,terms}`, `/[locale]/promoter/{login,apply,dashboard,events,new,events/[id],billing}`, `/[locale]/admin/{dashboard,promoters,revisions,transfers,emergency-hides,comedians,reports}`. More detailed route-by-route specification in `docs/FRONTEND_UX.md`.

**Home:** top bar logo/EN-ES/city select; location permission only on explicit user action; filter chips `Today`, `This weekend`, `Date`, `Show language`, `Category`, `Free/Paid`; responsive map and list toggle; user can browse even if geolocation declined or map fails. Map markers cluster at lower zoom, reveal occurrence card; cards show actual local date/time, venue, title, category/language, paid/free and tickets CTA. Never put clickable ads over map controls.

**Event page:** poster with alt text, original approved show title/description, approved venue address and map, all eligible occurrences, local time zone, lineup linking to published comedian profiles, admission price in event's ticket currency, approved external `Buy tickets / Entradas` HTTPS link and `Report issue`. Avoid showing unpublished lineups on a published page.

**Comedian page:** `/en/comedians/{slug}`, `/es/comedians/{slug}`; stage name/photo/native-language bio/home city/social links, and chronologically sorted **only eligible upcoming paid approved** appearances automatically linked through `EventPerformer`. Free profile, admin-managed MVP, safely empty if no upcoming shows, 404 for draft/hidden profiles. Admin creates/edits/publishes/hides with audit and image rights. **Do not give promoter or comedian users write access to profile fields in MVP.** Future phase: claimed profile with comedian identity proof, controlled ownership, moderated drafts, recovery and abuse appeals.

**Promoter:** dashboard status (`PendingApproval`, `Draft`, `InReview`, `ApprovedAwaitingPayment`, `Published`, `EditInReview`, `Hidden`, `Expired`, etc.), wizard create event (show, place, schedule, tickets, lineup, poster, review), recurring schedule preview, submission, diff and review status, $2/$5 payment only after approval, transfer and emergency-hide controls, receipts. Explain why an account/event cannot publish.

**Admin:** queues for promoter verification, event revision differences, transfer decisions, emergency hides, comedian CRUD and publication, duplicate/abuse reports and payment exceptions; all decisions require auditable reason. Cancellations prioritized. Provide review content + existing published side by side and payment status read-only; no exposed raw card details.

**Newsletter:** optional form email + city + preferred newsletter language, unchecked explicit consent, accessible double opt-in confirmation and unsubscribe; weekly schedule defined by city/locale, chronological eligible list, no unpublished entries; automatic retry and suppression.

## 7. API contract — implement before frontend integration

Base `/api/v1`, JSON camelCase, ISO dates, UTC instants (`Z`), separate `localDate`, `localStartTime`, `timeZoneId`. REST with a single OpenAPI contract in `contracts/openapi.yaml`. Pagination by opaque cursor; list endpoints cap results; consistently return Problem Details (`status/title/code/detail/traceId`), 401 unauthenticated, 403 forbidden, 404 unknown/nonpublic, 409 stale state/version, 422 invalid business rule, 429 rate-limited. All authenticated state changes require secure session and CSRF where relevant. Mutating payment endpoints require an idempotency key.

**Public**
- `GET /public/occurrences?city=SAN_DIEGO|TIJUANA&from=YYYY-MM-DD&to=YYYY-MM-DD&bbox=west,south,east,north&category=&showLanguage=&freeOnly=&cursor=&limit=` — map and list exact same IDs. Cap date window, bounding box area and number of results; return lat/lon and venue local time.
- `GET /public/events/{slug}` and `GET /public/events/{slug}/occurrences`.
- `GET /public/comedians?query=&city=&cursor=` for search (can launch with limited catalog), `GET /public/comedians/{slug}`, `GET /public/comedians/{slug}/occurrences`.
- `GET /public/venues/{slug}` optional P1.
- `POST /public/newsletter/subscriptions`, `POST /public/newsletter/confirm`, `POST /public/newsletter/unsubscribe`.
- `POST /public/events/{id}/reports` rate-limited.

**Identity**
- `GET /auth/me`; sign-up/login/email-verification/logout/reset endpoints by ASP.NET Core Identity. Use the ASP.NET Core Identity cookie login/verification/logout endpoints defined in the OpenAPI contract. Session cookie not accessible to JS.

**Promoter**
- `POST /promoter/application`, `GET /promoter/me`, `GET /promoter/events`, `POST /promoter/events` (private draft), `GET /promoter/events/{id}`, `POST /promoter/events/{id}/revisions`, `POST /promoter/revisions/{id}/submit` (two-step draft -> submit), `GET /promoter/revisions/{id}`.
- `POST /promoter/listings/{entitlementId}/checkout` (200/500 USD total, one-time) and `GET /promoter/orders/{id}`; `POST /promoter/listings/{id}/transfers`; `POST /promoter/occurrences/{id}/emergency-hide`; `POST /promoter/media/presign-upload` after ownership and type validation.

**Admin**
- `GET /admin/overview`; `GET /admin/promoters/pending`, `POST /admin/promoters/{id}/decision`; `GET /admin/revisions/pending`, `GET /admin/revisions/{id}/diff`, `POST /admin/revisions/{id}/decision`; `GET /admin/transfers/pending`, `POST /admin/transfers/{id}/decision`; `GET /admin/emergency-hides/pending`, `POST /admin/emergency-hides/{id}/decision`; `GET/POST /admin/comedians`, `GET/PATCH /admin/comedians/{id}`; `GET /admin/reports`; `GET /admin/orders/exceptions`. Admin writes are strictly RBAC + audited.

**Integrations**
- `POST /webhooks/stripe`: raw body, verify `Stripe-Signature`, store unique event, process asynchronously or synchronously in idempotent transaction. Never expose webhook publicly for operations beyond provider verification.
- Internal worker jobs for newsletter, expiry, failed payment reconciliation, occurrence horizon, purge and email outbox.

Use original v5 API as a starting reference, but `contracts/openapi.yaml` v6 is authoritative. Response examples, domain errors and pagination semantics in `docs/API_AND_FRONTEND_CONTRACTS.md`.

## 8. Payment/tax engineering — fail closed

Server creates two immutable priced products/Prices with USD `tax_behavior=inclusive`: 200¢ and 500¢. Stripe's USD default `automatic` is normally tax-**exclusive**, so explicitly set inclusive behavior; confirm product tax code and registrations with qualified advisor. The tax-inclusive gross amount is contractually fixed. Before redirecting and after Checkout completion confirm actual `currency='usd'` and `amount_total=200/500`, and capture tax allocation/refund fields. If configured Stripe Checkout would present higher total, **do not create/activate** that checkout; log alert and return `CHECKOUT_TAX_CONFIGURATION_INVALID`. Do not hardcode assumed tax rates, guess Stripe tax codes or promise tax exemption. For international payment methods, enable only methods that support the locked USD total in your Stripe setup.

**Webhook handling:** validate raw payload signature with official SDK; enforce dedupe by Stripe event ID and payment/session IDs; verify the checkout session/order mapping, paid state, currency and full amount; activate entitlement in one DB transaction; publish only from revalidated eligible projection. Handle asynchronous payments, expiry, refunds, chargebacks, out-of-order delivery, and manual replay. Receipt/customer PII minimized. Platform fees are not tickets. Reconciliation worker fetches missing/ambiguous Stripe statuses. Never accept `success_url` as fulfillment proof.

**Refund policy:** product owner/legal sign-off required; implement explicit admin refund reason flow and accurate state after refund/dispute, not arbitrary no-refund assumptions. If a refund requires revocation, suppress entitlement immediately in eligibility query. Cancellations/transfers must not secretly issue/refund payments.

## 9. Public imports: intentionally private

Sources (e.g. licensed event data APIs) require checked terms and rights. `ImportSource` documents permissions, origin, retrieval time, retention and allowed fields. `ImportCandidate` can suggest organizers or speed up manual promoter submission but is **never eligible** for public APIs, sitemap, search, map, list, newsletters, or artist appearances. Only a verified promoter can own an event, and the normal approved + paid flow applies. Avoid scraping/republishing posters/descriptions without permission. Adapter integration is P1; do not block MVP launch on API inventory.

## 10. Performance, accessibility, SEO and analytics

**Performance:** mobile-first; route-based SSR content with real text, not a blank JS-only map; map dynamically imported; <=2.5s LCP as aspirational budget under pilot mobile conditions; optimize images, paginate occurrences, cluster map, bounds debounce, prefetch details conservatively; cache public eligibility no longer than ~30–60 s unless invalidated on hide/moderation/payment changes. Ensure emergency hide is authoritative at API level **immediately**, even when CDN is stale (purge/invalidate; no long-lived cached map results).

**Accessibility:** WCAG 2.2 AA target; keyboard-operable filters, list alternative to map, labels/errors/live regions, minimum tap targets, reasonable focus order, screen-reader announcements for list changes, `lang` attributes, reduced motion.

**SEO:** canonical URLs for events and comedians; `/en` and `/es` with `hreflang` alternates; Event JSON-LD only for eligible real performances, never invented reviews/ratings; noindex private, unpaid, expired (unless historical page is intentionally retained), or draft content; sitemap contains only public entries. Profiles can be indexed without an upcoming show; marked-up event appearances still require public eligibility. OpenGraph previews with safe images. Avoid auto-translating organizers' original show text.

**Analytics:** collect aggregate page views, map-to-detail transitions, outbound ticket-link clicks, newsletter signups, promoter publish/payment funnel and operational events; honor privacy choices, do not expose attendees or private promoter identity in analytic events. No ad instrumentation or AdSense initially required.

## 11. Security, privacy, abuse prevention and reliability

- Session auth + email verification; admin TOTP MFA; roles enforced in API policies. Permissions per promoter and resources; deny direct-ID access to others' drafts and payments.
- Hash secret tokens; protect against CSRF, XSS, SQL injection, open redirects, SSRF on imports; validate URLs; rich text sanitize or render plain text; rate-limit spam/reporting/login.
- Poster uploads: presigned auth, size/type restrictions, content inspection/scanning, EXIF stripped, web-safe derivative; remove orphan uploads; copyrighted/promotional image rights declaration. No public raw uploads until approved.
- Audit all approvals/rejections/suppressions/transfers/refunds/profile publication and administrative overrides. Keep before/after revision IDs and reason; log correlation IDs, avoid logging tokens or full PII.
- Health checks `/health/live` and `/health/ready`, database migration status, worker job failures, Stripe inbox dead letters, moderated queues, email bounces, backups and restore drill. Separate staging from production Stripe/webhook secrets and DBs.
- Privacy and marketing law: plain-language notices for CA and MX; double opt-in; email unsub token, suppression list; consent timestamp/source; delete/export workflow subject to legal requirements. Age-targeting not intended: general audiences, avoid collecting child info.
- Domain/links: validate actual available domains independently. `radarcomedy.com`/`radarcomedia.com` are **proposed examples**, not verified ownership. Only point DNS after domain acquisition/verification.

## 12. Feature-specific edge cases

**No events:** show welcoming empty state with chosen city/date and newsletter CTA, not arbitrary free imported events. **Multiple shows same venue:** distinct pins cluster/stack, do not collapse to one event. **DST:** local date and time must not shift because device location differs; venue zone explicit. **Cross-border:** language, country, event ticket currency independent. **Multi-lineup:** one comedian may appear on multiple distinct events and occurrences; event occurrence lineup may change by approved revision. **Profile privacy:** never auto-claim a real person based on a social URL; admin publication requires rights/consent review. **Past shows:** do not allow a completed single-event entitlement to be reused. **Checkout abandonment:** remain approved-awaiting-payment with retry; no public leak. **Refund dispute:** fail closed. **Venue move:** require reviewed edit and production identity validation; if fundamentally different production, charge new listing. **Recurring renewals:** new manual purchase for next window; no overlapping entitlements; existing series/slug can persist with a fresh entitlement.

## 13. Delivery milestones and release gates

1. **M0 — Foundation:** repo structure, CI, Compose, Postgres/PostGIS, migration owner, auth/email, admin TOTP MFA and unit tests.
2. **M1 — Content & approvals:** promoter onboarding, venue, comedian admin CRUD, events/revisions, moderation queue, recurrence engine and domain tests.
3. **M2 — Public experience:** EN/ES React/Next routes, responsive map and list, event/comic pages, filter parity, SEO and accessibility.
4. **M3 — Billing & publish:** Stripe tax-inclusive configured price, checkout, webhook inbox, entitlement activation, emergency hide and transfers; end-to-end paid publication.
5. **M4 — Retention & hardening:** double-opt-in newsletters, private imports if permitted, backups, security testing, monitoring, legal/tax signoff, production readiness.
6. **M5 — Pilot:** recruit first promoters, populate paid inventory, measure funnel, fix errors; AdSense and self-service comedian profile management remain future work.

**Production release gate:** no launch unless all P0 tickets' acceptance criteria pass, including server-side public eligibility, tax-inclusive fixed totals, webhook replay, 10+ approved one-time transfers, out-of-term recurring rejection, admin edit moderation, promoter approval, emergency hide and restore, comedian admin/profile appearance rules, accessibility map fallback, newsletter unsub, backup restore and legal/tax review. See `docs/TICKETS_AND_ACCEPTANCE.md`.

## 14. Citation / implementation references

- Stripe inclusive Price behavior: https://docs.stripe.com/tax/products-prices-tax-codes-tax-behavior
- Stripe Checkout fulfillment: https://docs.stripe.com/checkout/fulfillment.md?ui=full-page
- Stripe webhook verification: https://docs.stripe.com/webhooks
- EF Core + PostgreSQL NTS/PostGIS: https://www.npgsql.org/efcore/mapping/nts.html
- Next.js App Router internationalization: https://nextjs.org/docs/app/guides/internationalization
- Next.js dynamic metadata/SEO: https://nextjs.org/docs/app/getting-started/metadata-and-og-images
- WCAG 2.2: https://www.w3.org/TR/WCAG22/
- RFC 5545 calendar recurrence: https://www.rfc-editor.org/rfc/rfc5545

**Handoff instruction:** Follow `docs/AGENT_EXECUTION.md` for agent behavior and exact completion report format. This spec defines expected behavior but does not mean the app already exists.



---

## PART 2: docs/BACKEND_AND_DATA.md

# Backend, EF Core, database and worker implementation

**Authority:** `../MASTER_SPEC.md`. These details are implementation design, **not a compiled codebase**. Implement against `../contracts/openapi.yaml` and add actual EF migration classes + tests. Replace any incompatible v5 `reference/` examples with equivalent v6 behavior rather than copying them blindly.

## 1. Project structure and dependency direction

```text
src/RadarComedy.Domain/              # pure C# domain, entities, value objects, invariants
src/RadarComedy.Application/         # commands/queries, ports, validation, authorization intent
src/RadarComedy.Infrastructure/      # EF Core Npgsql, Stripe, mail, object store, time service
apps/api/RadarComedy.Api/            # ASP.NET Core .NET 10 minimal API/controllers + auth
apps/worker/RadarComedy.Worker/      # expiry, outbox, reconciliation, newsletter
```

Dependency direction: `Domain <- Application <- Infrastructure`; API/Worker compose Application and Infrastructure through DI. Avoid unrelated microservices. Handlers may use EF DbContext directly with explicit policies; no mandatory generic repository abstraction. `CancellationToken` on all async IO. `DateTimeOffset`/UTC `DateTime` only for instants; `DateOnly` + `TimeOnly` + IANA zone for schedules. Inject `TimeProvider` for deterministic tests. Use `IOptions<T>` with validation at startup; no secrets in source.

Recommended application services and responsibilities:
- `IAuthService`: registration/verification/session/logout, role lookup; use ASP.NET Core Identity with cookie auth.
- `IPromoterApprovalService`: verified evidence, status transitions, audit, notifications, suspension.
- `IEventCommandService`: owner checks, create private draft, submit immutable revision, validate comedian/venue/recurrence.
- `IModerationService`: atomic approve/reject/changes requested, diff, stale-revision conflict, event suppression.
- `IRecurrenceService`: generate/preview weekly/daily/monthly occurrences, local-to-UTC, DST policy and fixed paid window.
- `IPublicDiscoveryService`: exclusively reads shared eligibility SQL/query; builds map/list profile/search/SEO/newsletter projections.
- `IListingService`: plan/cost/term/transfer/expiry; immutable pricing and payment eligibility.
- `IPaymentService`: create Stripe session with idempotency key, webhook verify/dedupe, refunds/disputes/reconcile.
- `IComedianProfileService`: admin CRUD, publication audit and appearance resolver, no self-claim until post-MVP.
- `INewsletterService`: consent, token lifecycle, segment generation, message suppression and unsubscribe.
- `IMediaService`: pre-signed upload and publish vetted derivatives.

## 2. Strong types/enums

```csharp
public enum Role { Fan, Promoter, Admin }
public enum PromoterStatus { PendingReview, Approved, Rejected, Suspended }
public enum EventFormat { Single, Recurring }
public enum EventCategory { StandUp, OpenMic, Improv, Sketch, ComedyTheater, Festival, OtherLiveComedy }
public enum EventRevisionStatus { Draft, Submitted, ChangesRequested, Approved, Rejected, Superseded }
public enum OccurrenceStatus { Scheduled, Canceled, Postponed, Completed }
public enum ListingPlan { Single, RecurringThreeMonths }
public enum EntitlementStatus { PendingPayment, Active, Suspended, Expired, Revoked }
public enum ComedianProfileStatus { Draft, Published, Hidden }
```

Persist enums as constrained strings, not numeric ordinals. All input DTOs use explicit validators; domain objects cannot be created in impossible published states. Persist all-in price in **minor units** as `int`; amount 200/500 `USD`. Admission/ticket price is separately stored, can be `USD` or `MXN`, and has **no relationship** to listing fee. Do not put credit cards or personal attendees in app database.

## 3. EF Core model and canonical PostGIS configurations

Use `Npgsql.EntityFrameworkCore.PostgreSQL`, `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite`, `NetTopologySuite`, and optionally `Npgsql.EntityFrameworkCore.PostgreSQL.NodaTime` if standardized across project. Keep major versions compatible with .NET 10 and EF Core version actually installed. Register `UseNpgsql(connection, x => x.UseNetTopologySuite())` and `HasPostgresExtension("postgis")` in model.

**Example mapping (illustrative, integrate into actual DbContext):**

```csharp
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

protected override void OnModelCreating(ModelBuilder b)
{
    b.HasPostgresExtension("postgis");
    b.Entity<Venue>(e =>
    {
        e.ToTable("venues");
        e.HasKey(x => x.Id);
        e.Property(x => x.Location).HasColumnType("geography (point, 4326)");
        e.HasIndex(x => x.Location).HasMethod("gist");
        e.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
    });
    b.Entity<ComedyEvent>(e =>
    {
        e.ToTable("events");
        e.HasKey(x => x.Id);
        e.HasIndex(x => x.Slug).IsUnique();
        e.Property(x => x.Version).IsConcurrencyToken();
        e.Property(x => x.Format).HasConversion<string>();
        e.Property(x => x.Category).HasConversion<string>();
    });
    b.Entity<EventRevision>(e =>
    {
        e.ToTable("event_revisions");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.EventId, x.RevisionNumber }).IsUnique();
        e.Property(x => x.Status).HasConversion<string>();
        e.Property(x => x.SnapshotJson).HasColumnType("jsonb");
    });
    b.Entity<Comedian>(e =>
    {
        e.ToTable("comedians");
        e.HasKey(x => x.Id);
        e.HasIndex(x => x.Slug).IsUnique();
        e.Property(x => x.ProfileStatus).HasConversion<string>();
    });
    b.Entity<EventPerformer>(e =>
    {
        e.ToTable("event_performers");
        e.HasKey(x => new { x.OccurrenceId, x.ComedianId });
    });
}
```

**Spatial semantics:** NTS Point `(X=longitude,Y=latitude,SRID=4326)`, never swap lat/lon. `geography` calculates earth-distance in meters; index with GIST; avoid performing `Distance` in .NET on fetched `geometry` and claiming meters. For bounding boxes (including anti-meridian logic if expanding beyond launch), use indexed PostGIS query. Limit query windows, result cardinality and geometry size.

## 4. Migration plan and constraints

**One migration authority only:** recommended EF Core migrations with committed `ModelSnapshot`. Old files under `reference/db/` are SQL design references, not an extra install step. EF migration chain should implement:

| Migration | Key operations |
|---|---|
| `20261118_InitialIdentityCatalog` | PostGIS extension, users/roles, promoters, venues, events/revisions, reference lookups; unique and FK constraints |
| `20261118_ListingAndBilling` | occurrences, recurrence schedule/exceptions, listings, checkout orders, Stripe inbox, term overlap prevention, partial uniqueness |
| `20261118_ModerationAndComedians` | transfer/hides/audits, comedian/profile and lineup, indexes, publication state constraints |
| `20261118_NewsletterAndImports` | subscribers/suppression, import source/candidates, outbox and reporting |
| `20261118_PublicEligibility` | SQL view/query and supporting indexes, no public-unpaid imports |

Migration IDs above are human-readable suggestions; let EF generate actual timestamped filenames and consistently commit C# + snapshot. A production DB already using `reference/db/V001-V003` needs a separate **baseline/reconciliation** plan; do not rerun create-table migrations against existing tables.

Commands (after projects are created):

```bash
dotnet ef migrations add InitialIdentityCatalog \
  --project src/RadarComedy.Infrastructure \
  --startup-project apps/api/RadarComedy.Api

dotnet ef database update \
  --project src/RadarComedy.Infrastructure \
  --startup-project apps/api/RadarComedy.Api
```

At deploy, use one controlled migrator job with backup/lock and clear rollback strategy; do not let multiple API replicas auto-run incompatible migrations concurrently. Add migration smoke tests to a fresh PostgreSQL+PostGIS service, confirm indexes and view definitions, and run SQL/EF model parity tests.

**Critical DB checks**

```sql
-- If a recurring performance is scheduled, at most one per local date/event.
CREATE UNIQUE INDEX IF NOT EXISTS uq_one_scheduled_occurrence_per_event_date
ON event_occurrences (event_id, local_date)
WHERE status = 'Scheduled';

-- Prevent event revisions with duplicate revision numbers.
CREATE UNIQUE INDEX IF NOT EXISTS uq_event_revision_number
ON event_revisions(event_id, revision_number);

-- Webhook replays have one durable id.
CREATE UNIQUE INDEX IF NOT EXISTS uq_stripe_webhook_event
ON stripe_webhook_inbox(stripe_event_id);
```

More constraints: performer composite PK `(occurrence_id,comedian_id)`; streamer? **No**; no repeated active overlapping recurring terms, no second paid order per entitlement, numeric cents only 200/500, valid UTC ranges, legitimate HTTPS public URLs. Validate foreign keys and entity ownership in services and database. SQL checks alone cannot enforce "same production"; make that an explicit manual review criterion.

**Important reference-code limitation:** old reference `ListingEntitlement` has `SingleOccurrenceId` required even while some draft creation flows have not yet materialized occurrences. Implement draft event + occurrence projection before entitlement creation or explicitly model a pending reference that becomes mandatory before payment. Do not loosen public eligibility just to allow a draft flow.

## 5. Public eligibility is a security boundary

One source of truth (SQL view or composable EF query). Conceptual SQL:

```sql
SELECT o.id, o.event_id, o.local_date, o.starts_at_utc, o.venue_id
FROM event_occurrences o
JOIN events e ON e.id = o.event_id
JOIN promoters p ON p.id = e.promoter_id
JOIN event_revisions r ON r.id = e.published_revision_id
JOIN listing_entitlements le ON le.event_id = e.id
WHERE p.status = 'Approved'
  AND r.status = 'Approved'
  AND le.status = 'Active'
  AND e.visibility_suppressed_at IS NULL
  AND o.status = 'Scheduled'
  AND o.emergency_hidden_at IS NULL
  AND o.published_revision_id = e.published_revision_id
  AND (
       (le.plan = 'Single' AND le.single_occurrence_id = o.id)
    OR (le.plan = 'RecurringThreeMonths' AND o.local_date >= le.term_start_local_date
       AND o.local_date < le.term_end_exclusive_local_date)
  );
```

This query is **conceptual**; production implementation must ensure event+revision relationship integrity, entitlement is *actually* settled by unique verified paid order (or strongly invariant-backed `Active` state), occurrence is not stale due to a revision swap, and latest payment dispute/revocation suppresses results. Some revision changes may retain existing occurrence IDs; query should not depend on an obsolete `published_revision_id` if the occurrence is intentionally preserved. Wrap publication updates in transactions; explicitly test each case. `public_eligible_occurrences` must supply the ID set for map/list/event pages/comedian appearances/SEO/newsletters. Never expose private import candidates even via generic search routes.

## 6. Moderation and event revision algorithm

**On promoter application:** normalized email with verified timestamp; valid HTTPS organizer URL; rate-limit application; admin manually reviews organizer relationship and records reason. Only approved promoter can create/submit. Promote role in DB and policy after approval; no client-supplied role flag trusted.

**On create event:** validate promoter ownership; create event with `PublishedRevisionId=null`; create `EventRevision` #1 Draft with complete snapshot including poster media keys and appearance references; save private. `POST /revisions/{id}/submit` locks snapshot and changes status Submitted. Each snapshot validated independently; cannot rely on client preview for recurrence. Admin approves with `If-Match`/revision version check. Before payment, `PublishedRevisionId` may point at approved draft but event remains private. Later edit creates a cloned immutable snapshot and another revision number; approval atomically swaps publication pointer, refreshes materialized occurrences and invalidates caches.

**On transfer:** proposed revision/transfer referencing exactly one current scheduled incomplete occurrence + one entitlement. Check same show and organizer. Admin approves in transaction with row/version lock, marks old occurrence canceled/replaced, creates or updates target occurrence; if this creates a simultaneous second public occurrence fail transaction. Each approval must add moderation event with reviewer/time/reason. No arbitrary counter cap.

**On recurring schedule:** preview rules independently of paid entitlement. After approval first occurrence anchor and three-month end are frozen in entitlement. A schedule edit can remove/move occurrences only inside term. Don't rewrite past completed occurrences. Normalize idempotent occurrence identity by `(event_id,original_schedule_local_date)` plus exception mapping, not just generated position.

**On emergency hide:** authorization `ownerOf(occurrence.event.promoter)` (or admin); set hidden timestamp immediately and queue review; safe if called repeatedly; no possibility for promoter to unhide via ordinary edit or toggle.

## 7. Recurrence algorithm and time zone

Support daily/weekly/monthly at launch but **at most one occurrence per local day**; allow one-off event instead of trying to shoehorn a festival into a recurrence. FirstLocalDate is the anchor; `endExclusive=firstLocalDate.AddMonths(3)`. Frequency and weekdays selected in promoter wizard, with explicit venue-local start, duration (for example max 24 hours), and optional approved per-date ticket links and lineup overrides. For every projected local date `d`: check `termStart <= d < termEnd`, no duplicate, exception skip/move, IANA timezone validity; resolve zoned local time with documented DST rule; create UTC instants with end > start. Materialize entire paid 3 months (daily max ~92 dates) at checkout/publish; immutable past occurrence records retained. For unpaid far-future drafts, generate bounded preview (e.g. 20 items) without blocking arbitrary start year if date representable. If no occurrence before endExclusive or first date invalid, reject input with clear 422.

**Required clock tests:** 2027-01-10 -> 2027-04-10 exclusive; February 28/29 and Jan 31 month clipping; DST gap/fall-back in both IANA zones; move target exactly exclusive end rejected; second show same day rejected; separately ticketed Thursdays allowed; unrelated festival shows rejected as one series; one-time transfer 25 times prior to completed still one active occurrence.

## 8. Stripe integration details

Use Stripe-hosted `mode=payment`, never `mode=subscription`. Store immutable server-generated plan price by entitlement and order; supply required billing address/tax details according to Stripe configuration; explicitly tax-inclusive USD Price. For session creation set Stripe idempotency key derived from `checkout_order_id` plus attempt. Repeated HTTP requests should return existing open session or a safe new order only if previous has definitively expired; protect against two settled orders for one entitlement.

Webhook endpoint reads raw body bytes before parsers, validates with official Stripe signature secret, stores unique Stripe event ID; respond promptly after durable acknowledgment. Processor checks source account, Checkout session ID, metadata order ID, currency, `amount_total`, `payment_status`, event class and corresponding entitlement. Atomically mark paid/order and activate entitlement with unique paid order; enqueue cache invalidation and notification via outbox. Handle `checkout.session.completed` (only if paid), `checkout.session.async_payment_succeeded`, failed/expired, `charge.refunded`/`refund.*` and disputes. Map exact provider events to state with versioned docs for SDK chosen. Reconcile directly with Stripe for timeouts, missing webhooks or inconsistent statuses. Never let webhook processing set a duplicate entitlement active.

For all-in tax: enforce `total=200` or `500` USD cents pre- and post-payment; store `tax_included_cents` from Stripe. Product tax code/registration must be configured by human; do not guess. Developer tests should run a mocked payment provider; staging uses Stripe test mode and webhook CLI. Production requires owner approval and tax review.

## 9. Promoter, admin and comedian profile permissions

| Operation | Fan anonymous | Pending promoter | Approved promoter | Admin |
|---|:---:|:---:|:---:|:---:|
| Public browse/list/comic profile | ✓ | ✓ | ✓ | ✓ |
| Newsletter subscribe | ✓ | ✓ | ✓ | ✓ |
| Submit promoter application | — | ✓ | — | — |
| Create/submit/edit own event draft | — | — | ✓ | ✓ |
| Initiate own payment | — | — | ✓ | ✓ (audited) |
| Request own transfer | — | — | ✓ | ✓ |
| Emergency-hide own occurrence | — | — | ✓ | ✓ |
| Approve promoter/revisions/transfers | — | — | — | ✓ |
| Create/edit/publish/hide comedian | — | — | — | ✓ |
| Claim/self-edit comedian profile | — | — | — | — (deferred entirely) |

Role checks must also ensure ownership. Promoter cannot edit another organizer's event even if ID is guessed. Admin publication of comedian bio, portrait, social links requires rights/impersonation considerations; profile status `Published` only after explicit review and audit. Profile visibility independent of listing payment but **appearance list** must be paid+approved eligible. Delete/hide request should be available through published support channel; self-claim future only.

## 10. Email and background jobs

- Email: verify account, promoter approved/rejected, event revision approved/rejected, checkout available, paid receipt/confirmation, emergency hide alert, transfer decision, newsletter confirmation/unsubscribe.
- Queue outbox in same DB transaction as state changes; worker leases jobs with retries, jitter/backoff, idempotency keys, dead-letter status, alerting.
- Newsletter schedule: one weekly campaign per city+language, in city local timezone; configure send day/time without hard-coding product policy. Query only future eligible public events, no paid/hidden draft leakage. Use explicit double opt-in and suppress unsubscribed/bounced. Use a robust transactional/mass-mail provider instead of direct blocked SMTP login.
- Expiry: recurring entitlement is active for its local term (even if its activation UTC instant is earlier due to advance purchase). **Do not incorrectly expire a far-future prepaid series at payment time.** Distinguish `paid/active` entitlement and `term currently in coverage`; public future occurrences in the paid term may be listed in advance, according to date filters. At/after term end and no renewed term, no future eligible occurrences.
- Backups: DB daily + object storage lifecycle, encrypted off-host backup, tested restore, schema migration control, health/metrics.

## 11. Endpoint / error test matrix

`401 AUTH_REQUIRED`, `403 PROMOTER_NOT_APPROVED`, `403 RESOURCE_NOT_OWNED`, `409 REVISION_CONFLICT`, `409 LISTING_ALREADY_PAID`, `422 OCCURRENCE_OUTSIDE_ENTITLEMENT_TERM`, `422 SAME_PRODUCTION_REQUIRED`, `422 DUPLICATE_OCCURRENCE_DATE`, `422 SHOW_ALREADY_COMPLETED`, `422 INVALID_TIME_ZONE`, `422 CHECKOUT_TAX_CONFIGURATION_INVALID`, `422 INVALID_TICKET_URL`, `429 RATE_LIMITED` as stable `code` in Problem Details. No client can supply/modulate these business flags. Every state transition tests unauthorized, valid, repeated/idempotent, stale concurrent and rollback cases. See `TICKETS_AND_ACCEPTANCE.md`.

## 12. Production hardening checklist

Promoter/admin auth, MFA/CSRF, no insecure CORS, signed webhook, write idempotency and transactional outbox, encrypt secrets, tested backup/restore, reasonable retention, slow-query telemetry, abuse rate limits, image content validation, no paid/unpaid data leakage through CDN/sitemap/SEO, web availability and worker alerts, database migration/test deploy, legal/tax decision gates, per-country public notices, support process for false or impersonating comedian profiles.



---

## PART 3: docs/FRONTEND_UX.md

# React / Next.js frontend implementation specification

**Authoritative product policies:** `../MASTER_SPEC.md`. Build a polished, responsive PWA that remains useful without account/login, location permission, installed app, or functional map tiles. Use Next.js App Router, React, TypeScript strict and route-level SSR for SEO; MapLibre is a dynamically imported client-only component.

## 1. Experience principles

- **Three-tap core goal:** choose a city, find upcoming comedy, tap a show and follow approved external ticket link.
- **Map and list parity:** same API result set/filters; map is visualization, list is equally first-class accessible navigation.
- **Bilingual UI:** English and Spanish toggle in header; city does not determine locale; the original event description, comedian name, and original profile bio are *not* automatically translated. Ticket currency and venue local date/time are separate preferences.
- **Trust and clear state:** public items are paid/approved; show accurate local dates/venues; notices for temporarily unavailable/canceled events; promoter and admin never see fake success state.
- **Fast on poor mobile connections:** skeleton loading, pagination, low-overhead clusters, optimized media, no blocking map hydration; SEO event pages server-render content.
- **Responsive:** design for 360–430px phones, 768px tablets, 1200px desktops, tested to 320px without horizontal overflow.

## 2. Route map

App router file layout (example):

```text
apps/web/src/app/
  layout.tsx                   # metadata, global styles, error boundaries
  [locale]/
    layout.tsx                 # validate en/es, global nav, translations
    page.tsx                   # discovery home: map/list
    events/[slug]/page.tsx     # public event page (server-rendered)
    comedians/page.tsx        # catalog/search (MVP simple)
    comedians/[slug]/page.tsx # comedian profile
    newsletter/page.tsx       # optional subscription landing
    subscribe/confirm/page.tsx
    unsubscribe/page.tsx
    promoter/
      login/page.tsx
      apply/page.tsx
      dashboard/page.tsx
      events/new/page.tsx
      events/[id]/page.tsx
      billing/page.tsx
      checkout/success/page.tsx
      checkout/cancel/page.tsx
    admin/
      page.tsx
      promoters/page.tsx
      reviews/page.tsx
      reviews/[revisionId]/page.tsx
      transfers/page.tsx
      emergency-hides/page.tsx
      comedians/page.tsx
      comedians/new/page.tsx
      comedians/[id]/page.tsx
      orders/page.tsx
    legal/privacy/page.tsx
    legal/terms/page.tsx
  manifest.ts
  sitemap.ts
  robots.ts
```

`/` redirects based on saved preferred UI language if permitted, then `Accept-Language`, fallback `/en`. Spanish domain is an optional redirect to `/es` **only after verified ownership**. Canonical content routes stable across locales; use `hreflang` en/es alternates and avoid duplicate canonical errors. All admin/promoter paths guarded **server-side**, not just hidden menus.

## 3. React folder structure

```text
src/
  components/
    layout/{Header,LanguageSwitcher,CitySelector,Footer,MobileNav}.tsx
    discovery/{DiscoveryShell,FiltersBar,MapPanel,OccurrenceMarker,OccurrenceCard,
               ResultsList,MapListToggle,LocationButton,EmptyState}.tsx
    events/{EventHero,OccurrenceCalendar,Lineup,VenuePanel,TicketLink,ReportEvent}.tsx
    comedians/{ComedianHero,SocialLinks,UpcomingAppearances,ComedianList}.tsx
    promoter/{PromoterStatus,EventWizard,RecurrenceEditor,MediaUploader,
              ModerationTimeline,TransferDialog,EmergencyHideDialog,CheckoutButton}.tsx
    admin/{ReviewQueue,RevisionDiff,PromoterReview,ComedianEditor,
           EmergencyHideQueue,TransferReview,AuditTimeline}.tsx
    shared/{ErrorNotice,AsyncStatus,Dialog,FormField,AccessibleButton}.tsx
  features/{auth,discovery,events,comedians,promoter,admin,newsletter}/
  lib/{api-client,dates,formatters,locations,urls,analytics}.ts
  i18n/{en,es}.json
  styles/
```

Use **one generated API client** derived from `contracts/openapi.yaml`, e.g. `openapi-typescript` or Orval, keeping API/client types aligned. Use React Query/TanStack Query for browser-state fetch/cache/dedup; for SSR use server `fetch` or shared request abstraction and proper invalidation. Zustand only if genuinely useful for UI preferences such as map/list mode, not as authoritative source for Stripe or moderation state. React Hook Form + Zod or equivalent for instant UX validation; backend is final validator. Use a design system with WCAG-compliant colors and reduced motion.

## 4. Home screen / discovery (`/[locale]`)

**Mobile layout:** sticky header with brand + locale toggle; city selector; search/filter row; map taking ~40–55% of viewport on map mode; bottom sheet with scrollable event cards (or list-only mode). Desktop: two-column pane ~40% results + ~60% map with shared filters, optional drawer. Location permission requested *only* after clicking “Near me”. Denied permission: leave city selector, no block.

Suggested conceptual wireframes:

```text
MOBILE 390px
┌───────────────────────────────────────┐
│ 🎤 RadarComedy            EN/ES  ☰     │
│ 📍 San Diego ▾               Near me   │
│ Today  Weekend  Date  Language  More  │
├───────────────────────────────────────┤
│                MAP                    │
│  cluster   ○2    ○3      ○1           │
│           + / - / reset               │
├───────────────────────────────────────┤
│  14 shows   [Map]  [List]              │
│  [card] Fri 8pm · Venue · $10         │
│  [card] Sat 7pm · Venue · Free         │
└───────────────────────────────────────┘

DESKTOP
┌──────────────────────────────────────────────────┐
│ RadarComedy     City   Dates/Filters       EN/ES  │
├────────────────────┬─────────────────────────────┤
│ 14 results         │ map with clustered markers │
│ card 1             │                             │
│ card 2             │                             │
│ ...                │                             │
└────────────────────┴─────────────────────────────┘
```

**Filters:** `city` required, `from/to` local dates, category, show language (`en/es/bilingual/other`), freeOnly, optional bounding box. URL search params are the shareable source for filters: reload/back/forward preserves selection. Start with `Today`, `This weekend`, and `Next 7 days`; do not guess device-local dates for venue schedule. Query same endpoint for map/list and use stable occurrence IDs. Debounce map bounds 250–500 ms, cancel stale requests; cluster markers. `Next 7 days` is a UI choice, not irreversible business limit; server paginates.

**Occurrence card:** title, approved local date/time zone, venue/city, `showLanguage`, category, ticket admission price (USD/MXN or Free; no price if unknown), comedian short lineup if available, poster thumbnail if approved, details action. No fabricated ratings or “open now”. Listing price $2/$5 is not the fan's ticket price.

**Loading/errors/empty:** skeleton cards, independent map failure notice, retry, permission denied fallback, city with zero paid listings CTA subscribe, network offline last-known-state label **not** a promise of live availability, stale data explicitly marked. Never show unpaid events as “suggested” filler. Maintain scroll/focus on results switch.

## 5. Event detail (`/[locale]/events/[slug]`)

SSR public event from `GET /public/events/{slug}`. Must 404/non-index absent if unpaid, unapproved, suspended, hidden or import-only. Present approved poster with alt, show title, native-language description (badge for original language), category, organizer name if public, venue address and mini map, local time zone, occurrence dates sorted chronologically, per-date ticket URL and price with currency; lineups using approved performer references; social-share URL; report event action; follow ticket link in safe new tab (`rel="noopener noreferrer sponsored?"` only if actually sponsored; otherwise `noopener noreferrer`) and mark clearly external. Handle links missing/invalid by no disabled ghost CTA.

For *recurring* event, offer date selector; each date can have separate ticket URL and performer lineup. Date pages or query anchors for SEO can be generated later; do not create rich event structured data that merges separate ticketed performances incorrectly. Emergency-hidden occurrence must vanish from active selector immediately (eligible data); if all eligible occurrences hidden, public event should be 404/unlisted or display a clear unavailable/canceled page if policy permits indexing—never show actionable wrong ticket link. Retained historical pages may have `noindex` depending on SEO policy.

## 6. Comedian profiles (`/[locale]/comedians/[slug]`)

**This is in MVP.** Profile available free of listing fee and no comedian account needed. Admin creates and maintains it; it can be `Draft`, `Published`, `Hidden`. Only Published route returns 200.

Layout: hero with approved stage name, image (safe placeholder if missing), home city/country, original bio, primary/comedy languages, optional Instagram/TikTok/site HTTPS links, “Upcoming Shows” chronological cards grouped by month or city, contact/report-profile request link. Use `GET /public/comedians/{slug}` + `GET /public/comedians/{slug}/occurrences`. Appearances only from eligible paid/approved unsuppressed shows; if zero, show “No upcoming performances currently listed” not fake events. Links from each event lineup to this page. Unlinked contributor stage names can display as plain text until admin creates the profile; a promoter cannot silently create a public biography.

**MVP permissions:** Admin creates/edits/publishes/hides profiles and approves photos/social links, with impersonation/rights check and audit. Promoter may propose a stage name in event lineup moderation but cannot modify profile. Comedian claiming/verification of their own profile is deliberately **Phase 2**, implemented with separate identity validation, claim requests, conflict resolution, versioned draft edits and admin moderation. No hidden self-edit endpoint or “Coming soon” form collecting personal identity documents at MVP.

**SEO:** stable slug across UI languages; `hreflang` `en/es` on pages; canonical per locale; original bio remains original; schema Person only with verified factual info and no fabricated credentials; only relevant public upcoming Event schema linking to public event pages.

## 7. Promoter onboarding and dashboard

**Account flow:** visitor registers (email+secure password or vetted auth method), verifies email, applies with display/contact name, HTTPS organizer/social URL and city. Show `Pending admin approval` state and email confirmation; neither pending nor rejected/suspended user can submit events or pay. Admin reviews organizer evidence. API guards all routes regardless of UI.

**Dashboard:** `My events`, `Create listing`, `Promoter verification status`, `Payments`, `Revision requests`, `Transfers`. Each row displays show title, city, format, actual approval/payment/publication state, next performance date, action. Expose explicit states: `Draft`, `AwaitingReview`, `ChangesRequested`, `ApprovedAwaitingPayment`, `Published`, `EditPendingApproval`, `EmergencyHidden`, `TermExpired`, `Suspended`; derive from backend values, not freeform client flags.

**Event creation wizard (six steps)**
1. **Show basics:** title, original description, category, show language(s), optional promoter page, posters (permissions check).
2. **Venue:** choose existing venue or submit new venue for approval; address city/country, map pin, IANA zone shown; avoid auto-assuming time zone from user's device.
3. **Schedule:** single date and local time **or** recurring daily/weekly/monthly schedule + max once/day + 3 calendar months start on first date; preview the entire eligible interval, UTC time conversion server-validated. Promoter selects recurring only for same production.
4. **Tickets:** free/paid/unknown admission; currency USD/MXN; HTTPS external ticket URL for each relevant occurrence (fallback common URL permitted if explicitly approved); price for fan, not listing fee.
5. **Lineup:** search existing published comedian profiles; suggest unlinked names for admin reconciliation; attach per-date performers to occurrences if recurring. No direct artist profile edits.
6. **Review:** diff-ready preview, exact publication plan $2 or $5 USD all-in, rights/compliance checkbox, submit for admin approval. No upfront charge.

**Submission UX:** draft may autosave privately (debounced, with status); once `Submitted`, locked; admin can request changes, author creates new revision. `ApprovedAwaitingPayment` page shows immutable final price and button “Pay $2” / “Pay $5”; Stripe hosted checkout. After return, show “Processing payment / Awaiting confirmation,” poll backend order status; never immediately render “Published” until webhook-confirmed API state. Cancellation returns to approved unpaid state. Payment failure has retry. No subscriptions.

**Editing:** promoter opens existing approved show, sees public snapshot, proposes revised snapshot; every change (including typo) requires review. Show compare/diff + pending badge. Changes do not affect existing map until approval. Transfer flow specifically selects original occurrence and target date; unlimited requests, admin can approve/deny, no new charge for same unfinished show. Recurring move validates paid window and max once/day. Emergency cancel is intentionally separate prominent action: confirmation modal and reason -> instant hide response -> “Hidden pending admin review”. This is the only promoter change applied immediately, for safety.

## 8. Admin portal

**Dashboard metrics:** Pending promoter applications, pending event revisions, date transfers, emergency cancellation notifications (highest urgency), payment exceptions, reports, profile drafts. All counts from protected API; role+MFA server checked. Queue filters by city, status, age, assigned reviewer. Avoid raw untrusted HTML/links. All decision forms require reviewer reason and optimistic version.

**Promoter review:** verified email, organizer page (click-through safe), city, evidence, approve/request info/reject/suspend with reason and audit. Never let self-approval happen via user-submitted role.

**Event review:** original event preview and side-by-side new revision diff including venue/time/price/lineups/images/ticket URLs; production-identity checklist, duplicate check, recurrence eligibility and term preview, admin notes, approval/rejection/changes request. Collision and stale-version errors show conflict rather than overwrite.

**Transfers:** show old and proposed local times, difference, same show identity evidence, completed state, paid entitlement, recurring bounds, conflict; approve or reject. After approval immediate map projection update and audit. Many transfers allowed by rule, but flagged suspicious patterns can trigger manual escalation.

**Emergency hides:** priority alert, reporter/reason, affected occurrence, event/ticket details, actions “Confirm cancellation”, “Restore” (if false alarm, with reason), “Contact promoter”; no live-action restore without admin. Queue records who closed it.

**Comedian profile manager:** list by stage name/slug/status; create/preview/edit native bio, photo, home city, language, social URLs; publish/hide with explicit audit; merge true duplicates with redirect and stable slug handling if required; avoid publishing unconsented private imagery. Create profile under a standalone `Comedian` entity, **not nested inside promoter**, so multiple promoters can link the same comedian.

## 9. Newsletter experience

Header/footer CTA “Get weekly comedy near [City]” / “Recibe la cartelera semanal”. Form: email, city, language, explicit unchecked consent, privacy policy. Confirmation email and landing status. Don't leak whether email already subscribed; 202 always with generic message. Weekly email uses appropriate locale template but event titles stay original. Each item links to eligible event detail in website; unsubscribe is frictionless, token-based and permanent until new consent. Handle bounces/suppression and privacy requests. Never include unpublished or emergency-hidden events.

## 10. Localization matrix

UI copy keys in `i18n/en.json`/`i18n/es.json`, full parity. Examples:

| Key | English | Spanish |
|---|---|---|
| `nav.discover` | Discover comedy | Descubre comedia |
| `filter.today` | Today | Hoy |
| `filter.weekend` | This weekend | Este fin de semana |
| `filter.showLanguage` | Show language | Idioma del show |
| `filter.free` | Free entry | Entrada gratis |
| `event.tickets` | Get tickets | Ver boletos |
| `comedian.upcoming` | Upcoming performances | Próximas presentaciones |
| `comedian.empty` | No upcoming performances listed | No hay presentaciones próximas publicadas |
| `promoter.pending` | Pending admin approval | Pendiente de aprobación |
| `promoter.awaitingPayment` | Approved—payment needed to publish | Aprobado—falta pagar para publicar |
| `admin.emergency` | Emergency cancellations | Cancelaciones urgentes |

Use `Intl.DateTimeFormat(locale,{timeZone: venueTimeZoneId,...})` with server-supplied instant to avoid local device timezone shifts. `Intl.NumberFormat` with event admission currency and intended locale. Store date/time separately in API; never parse arbitrary `YYYY-MM-DD` as a JS instant at device midnight to render venue dates.

## 11. Component state and interactions

| Component | Input | Output/event | Empty/error handling |
|---|---|---|---|
| `CitySelector` | selected city | update URL filter | default San Diego; supports Tijuana |
| `DateRangeFilter` | from/to local dates | update URL / refetch | bad range validation |
| `MapPanel` | eligible occurrences | marker select -> details | offline/list alternative |
| `ResultsList` | same eligible occurrences | card click -> event | no events CTA newsletter |
| `ComedianHero` | public comedian detail | social links | image/bio optional |
| `UpcomingAppearances` | public-eligible occurrences | event link | empty state, never imported/unpaid |
| `EventWizard` | private approved-promoter data | draft/revision submission | 422 field errors |
| `RecurrenceEditor` | local dates/venue zone | preview, validation | term boundary/DST warning |
| `AdminRevisionDiff` | base/current/new snapshots | approve/reject actions | 409 stale conflict refresh |
| `CheckoutButton` | approved entitlement | redirect to hosted checkout | expired/invalid tax config |
| `EmergencyHideDialog` | owned occurrence | immediate hide request | duplicate click idempotent |
| `NewsletterForm` | city/lang/email/consent | generic 202 | no email leak |

**UI test IDs:** prefer semantic roles and user text over brittle `data-testid`; use stable test IDs only for map canvas/markers where browser automation cannot click by accessible name reliably.

## 12. Client API, authentication and caching

`contracts/openapi.yaml` should generate TS typings; implement requests with `credentials:'include'` on same-origin `/api/v1`, CSRF header/token on mutations, correlation ID propagation. All unauthorized 401 should lead to login page *only on private routes*; public 404 must not redirect to promoter login. Abort stale map searches, deduplicate identical requests. Use SSR fetch with `no-store` or short revalidation for public visibility sensitive pages; PWA service worker may cache static JS/images but not stale map datasets or purchased eligibility. Emergency hide invalidation must take effect on next API response regardless of client state. Never optimistically mark paid/published after clicking checkout.

**Example typed shape (illustrative):**

```ts
type CityCode = 'SAN_DIEGO' | 'TIJUANA';
type ShowLanguage = 'en' | 'es' | 'bilingual' | 'other';
type OccurrenceSummary = {
  occurrenceId: string; eventId: string; slug: string; title: string;
  category: string; showLanguage: ShowLanguage;
  startsAtUtc: string; localDate: string; localStartTime: string;
  timeZoneId: string; venue: { venueId: string; name: string;
    cityCode: CityCode; latitude: number; longitude: number };
  ticketUrl?: string | null;
  admissionPriceCents?: number | null;
  admissionCurrency?: 'USD' | 'MXN' | null;
};
```

Define exact nullability in OpenAPI and use generated types rather than drifted hand-written DTOs. For known paid events with no ticket website, display “Contact venue / More details” rather than invalid URL; ticket URL optionality is a product-edge default that needs contract validation before live release.

## 13. Accessibility and responsive acceptance

Keyboard tab through header, city, filters, every result, event page, promoter form, admin review. Map has an always-available results list view, map filters have accessible labels, cards announce correct date/currency/venue, dialogs trap/restore focus and Esc to close where safe. Respect reduced motion and zoom up to 200%; target WCAG 2.2 AA and tests with axe + manual mobile screen reader spot checks. Prioritize actionable text over decorative emoji or large hero content. Color never sole carrier of moderation/payment status.

## 14. Testing and release conditions

- Vitest/Jest + RTL component tests for filters, locale switch, reusable cards/forms/diff/status.
- Playwright E2E tests for anonymous navigation without geolocation, mobile map/list parity, event→comedian linking, comedian no-upcoming case, promoter verification/approval, edit pending, checkout pending→webhook confirmation, 10+ transfers, emergency hide, admin queue, newsletter confirmation/unsubscribe.
- Run a fake Stripe provider in CI; execute staging Stripe test-mode webhook integration separately.
- Visual snapshot tests for 375px mobile, 768px and 1440px desktop; dark mode only if expressly implemented; no reliance on dark mode.
- SEO smoke tests: SSR title/description, hreflang, JSON-LD only public eligible, sitemap never unpaid, noindex private.
- Performance budgets and a11y regression thresholds in CI; not just screenshots of mocked app.

## 15. Post-MVP verified comedian self-management (design only)

New `ComedianAccountClaim` with `ComedianId, UserId, EvidenceUrl, VerificationStatus, ReviewedBy/At, AccessRevokedAt`. `ComedianProfileRevision` provides immutable proposed edits with admin review; role `Comedian` can only edit claims after verified ownership. A comedian may perform at multiple promoter shows and must not acquire editing access to those promoters' event content. Account recovery, impersonation disputes, duplicates/merges and transfer of ownership are explicit admin workflows. Public pages/URLs remain unchanged. Do not build this feature in first release.



---

## PART 4: docs/API_AND_FRONTEND_CONTRACTS.md

# HTTP API, request examples and frontend/backend contract

**Formal OpenAPI:** `../contracts/openapi.yaml` (v0.6). This file adds examples and validation rules; if paths or shapes differ, update both files before implementation. No API server is implemented in this package.

## Conventions

- Base path: same-origin `/api/v1` routed by Caddy to ASP.NET API; JSON `camelCase`, UTF-8.
- Dates `YYYY-MM-DD` venue-local (not JavaScript device timezone instants); times `HH:mm:ss`; UTC instants RFC3339 `Z`; timezone ID IANA; `X=longitude,Y=latitude` for PostGIS.
- Currency: platform listing price *only* USD 200 or 500 minor cents final tax-inclusive. Admission to event might be USD, MXN or free; do not display platform fee as ticket price.
- Auth: secure HttpOnly cookie; CSRF header for mutations; email verified for application; approved promoter for promoter modifications; admin+MFA for admin. Set `Cache-Control: no-store` on private/moderation and payment state routes.
- Pagination: opaque cursor, stable `nextCursor` for list; max 200 for public occurrences, smaller for admin. Filter limits, no unbounded all-venue lookups.
- Standard errors: `application/problem+json`: `{ "type": "https://.../problems/...", "title": "...", "status": 422, "code": "...", "detail": "...", "traceId": "..." }`. Public missing/hidden event returns 404 rather than reason for suppression.
- Idempotency: Checkout POST requires `Idempotency-Key`, admin decision and submit endpoints should accept idempotency/request IDs and optimistic `If-Match` when needed. Server authoritative status always overrides browser state.

## 1. Public discovery examples

```http
GET /api/v1/public/occurrences?city=TIJUANA&from=2026-11-19&to=2026-12-03&showLanguage=es&limit=50
```

```json
{
  "items": [{
    "occurrenceId": "1a62d1fc-5e1f-4e58-aad1-28bfbf2cee01",
    "eventId": "2a62d1fc-5e1f-4e58-aad1-28bfbf2cee02",
    "slug": "jueves-de-risas",
    "title": "Jueves de Risas",
    "category": "StandUp",
    "showLanguage": "es",
    "startsAtUtc": "2026-11-27T04:00:00Z",
    "localDate": "2026-11-26",
    "localStartTime": "20:00:00",
    "timeZoneId": "America/Tijuana",
    "venue": {
      "venueId": "3a62d1fc-5e1f-4e58-aad1-28bfbf2cee03",
      "name": "Example Venue — fictional",
      "cityCode": "TIJUANA",
      "latitude": 32.53,
      "longitude": -117.04,
      "timeZoneId": "America/Tijuana"
    },
    "ticketUrl": "https://tickets.example.org/show",
    "admissionPriceCents": 25000,
    "admissionCurrency": "MXN"
  }],
  "nextCursor": null
}
```

**Example is fictional**, including domain and show. Frontend must not assume admissions are USD just because $2/$5 listing fees are USD. A 20:00 Tijuana date shown on a San Diego phone remains the venue's intended local performance date/time.

`GET /public/events/{slug}` returns only approved **public** event and eligible occurrence set. On event detail render lineup links only if comedian profile is `Published`. `GET /public/comedians/{slug}` returns free public bio/profile; `GET /public/comedians/{slug}/occurrences` returns only paid, approved upcoming appearances. Profiles with zero upcoming shows can still render profile, never a fabricated show.

## 2. Comedian detail response example

```json
{
  "comedianId": "6a62d1fc-5e1f-4e58-aad1-28bfbf2cee06",
  "stageName": "María Ejemplo",
  "slug": "maria-ejemplo",
  "originalBio": "Comediante de stand-up en Baja California.",
  "photoUrl": null,
  "homeCity": "Tijuana",
  "countryCode": "MX",
  "primaryLanguage": "es",
  "instagramUrl": null,
  "tikTokUrl": null,
  "websiteUrl": null
}
```

No login required; no comedian profile payment. Admin-only create/modify/profile-status endpoints. Future verified claims explicitly deferred, do not add open write endpoint in MVP.

## 3. Promoter application

```http
POST /api/v1/promoter/application
Cookie: radar_session=...
X-CSRF-Token: ...
Content-Type: application/json
```

```json
{
  "displayName": "Comedy Producer Example",
  "contactName": "Example Organizer",
  "publicProfileUrl": "https://organizer.example.org",
  "cityCode": "SAN_DIEGO"
}
```

Response `201 {"id":"<uuid>","status":"PendingReview"}`. Email must be verified, URL must be HTTPS. Admin approves with audited review. A pending organizer creating event directly gets `403 PROMOTER_NOT_APPROVED`.

## 4. Draft a single event

```http
POST /api/v1/promoter/events
```

```json
{
  "title": "Independent Comedy Night",
  "description": "Stand-up event featuring local performers.",
  "category": "StandUp",
  "showLanguage": "bilingual",
  "format": "Single",
  "venueId": "3a62d1fc-5e1f-4e58-aad1-28bfbf2cee03",
  "firstLocalDate": "2027-02-14",
  "localStartTime": "19:30:00",
  "durationMinutes": 120,
  "ticketUrl": "https://tickets.example.org/night-1",
  "admissionPriceCents": 1500,
  "admissionCurrency": "USD",
  "performerIds": ["6a62d1fc-5e1f-4e58-aad1-28bfbf2cee06"],
  "posterKey": null
}
```

Response private draft with eventId/revisionId. Client submits review via `/promoter/revisions/{revisionId}/submit`; status moves Submitted. Only after admin approval can payment begin. Changes to approved event create *new* `POST /promoter/events/{eventId}/revisions` snapshot with `basedOnRevisionId`, never PATCH live event. Show explicit in-review status.

## 5. Recurring show and date-specific tickets

```json
{
  "title": "Weekly Open Mic",
  "description": "Same weekly production with rotating performers.",
  "category": "OpenMic",
  "showLanguage": "en",
  "format": "Recurring",
  "venueId": "3a62d1fc-5e1f-4e58-aad1-28bfbf2cee03",
  "firstLocalDate": "2027-01-07",
  "localStartTime": "20:00:00",
  "durationMinutes": 120,
  "ticketUrl": "https://tickets.example.org/weekly-show",
  "recurrence": {
    "frequency": "Weekly",
    "interval": 1,
    "daysOfWeek": ["TH"],
    "localStartTime": "20:00:00",
    "durationMinutes": 120,
    "timeZoneId": "America/Los_Angeles"
  },
  "occurrenceOverrides": [
    {"localDate":"2027-01-14", "ticketUrl":"https://tickets.example.org/weekly-show-jan14"}
  ]
}
```

Term: `[2027-01-07, 2027-04-07)`, local. Separate ticket URLs **do not** disqualify same recurring production from $5 plan. Backend validates approved venue zone matches schedule zone. Tickets with same site but different show heads/productions are not all one recurring series; admins must review identity. If an occurrence is moved to `2027-04-07`, return `422 OCCURRENCE_OUTSIDE_ENTITLEMENT_TERM`.

## 6. Checkout (no front-end payment fulfillment)

```http
POST /api/v1/promoter/listings/{entitlementId}/checkout
Idempotency-Key: 6eb8d8508c8a4d04ad4d
Cookie: radar_session=...
X-CSRF-Token: ...
```

```json
{
  "checkoutUrl": "https://checkout.stripe.com/c/pay/cs_test_...",
  "orderId": "7a62d1fc-5e1f-4e58-aad1-28bfbf2cee07",
  "totalCents": 500,
  "currency": "USD"
}
```

After Checkout redirect, GET `/promoter/orders/{orderId}` to read verified state; show “Payment processing” until server confirms. **Never publish from the redirect success query parameter.** Stripe webhook verifies and activates entitlement only if paid 500 USD cents final including tax. A 501-cent/other-currency checkout must fail closed. Provider fee and remitted tax come out of gross.

## 7. Modifications and emergency hide

```http
POST /api/v1/promoter/listings/{entitlementId}/transfers
```

```json
{
  "originalOccurrenceId": "1a62d1fc-5e1f-4e58-aad1-28bfbf2cee01",
  "proposedLocalDate": "2027-02-21",
  "proposedRevisionId": "8a62d1fc-5e1f-4e58-aad1-28bfbf2cee08",
  "reason": "Same production moved because the venue is unavailable."
}
```

202 pending, public date unchanged until admin approves. Unlimited approved same-show transfer requests allowed *before* performance completes. After completion: 422; recurring out-of-term: 422. Emergency hide is separate (`POST /promoter/occurrences/{id}/emergency-hide`); it suppresses right away and schedules admin review, even while ordinary content edits require approval. Admin has exclusive restore control.

## 8. Errors and authorization matrix

| Code | HTTP | Meaning |
|---|---:|---|
| `AUTH_REQUIRED` | 401 | Missing/invalid session |
| `PROMOTER_NOT_APPROVED` | 403 | Pending/rejected/suspended promoter |
| `RESOURCE_NOT_OWNED` | 403/404 | User cannot act on another organizer's resource |
| `REVISION_CONFLICT` | 409 | Stale review or outdated revision |
| `LISTING_ALREADY_PAID` | 409 | Duplicate payment request for active purchase |
| `OCCURRENCE_OUTSIDE_ENTITLEMENT_TERM` | 422 | Recurring move past 3 calendar months |
| `SAME_PRODUCTION_REQUIRED` | 422 | Reject transfer or recurring series bundling unrelated shows |
| `SHOW_ALREADY_COMPLETED` | 422 | Can't reuse completed $2 listing |
| `DUPLICATE_OCCURRENCE_DATE` | 422 | >1 scheduled show per local day for same series |
| `INVALID_TICKET_URL` | 422 | HTTP/nonapproved external URL |
| `CHECKOUT_TAX_CONFIGURATION_INVALID` | 422/503 | Checkout cannot guarantee exact all-in total |
| `RATE_LIMITED` | 429 | Client exceeded request frequency |

Missing/nonpublic public event/comic pages must return indistinguishable 404, not leak existence or moderator action. Private endpoints never cached publicly. Add `traceId` to error and logs without echoing PII.

## 9. Schema version and generated client

Treat `contracts/openapi.yaml` as checked-in, reviewed API design. In CI lint OpenAPI, generate TS client, fail if generated client differs or API integration tests contradict contract. Clearly mark provider-specific login endpoints not fully designed until ASP.NET Core Identity cookie implementation is selected. Do not fake implemented API endpoints simply because OpenAPI lists them. Add actual example responses from working integration tests during development, then freeze contract version for first MVP.



---

## PART 5: docs/TICKETS_AND_ACCEPTANCE.md

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



---

## PART 6: docs/DEPLOYMENT_RUNBOOK.md

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



---

## PART 7: docs/AGENT_EXECUTION.md

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

