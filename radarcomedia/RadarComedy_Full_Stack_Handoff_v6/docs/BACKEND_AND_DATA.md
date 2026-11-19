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
