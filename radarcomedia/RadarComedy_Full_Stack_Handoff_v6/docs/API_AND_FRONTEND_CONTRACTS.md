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
