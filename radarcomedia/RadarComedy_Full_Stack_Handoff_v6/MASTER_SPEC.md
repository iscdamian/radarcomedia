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
