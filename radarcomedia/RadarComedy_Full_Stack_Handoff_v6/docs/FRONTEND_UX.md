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
