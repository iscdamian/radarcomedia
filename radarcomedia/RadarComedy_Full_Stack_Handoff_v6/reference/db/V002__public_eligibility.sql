-- Only eligible records appear in public discovery, including events with paid recurring terms.
-- This VIEW does not protect tables on its own: the API's DB role must have restricted grants.
BEGIN;
CREATE VIEW public_occurrences AS
SELECT
  o.id AS occurrence_id, e.id AS event_id, e.slug, e.category,
  o.venue_id, v.city_code, v.location,
  r.snapshot_json->>'title' AS title,
  r.snapshot_json->>'showLanguage' AS show_language,
  o.local_date, o.starts_at_utc, o.ends_at_utc, o.ticket_url,
  o.admission_price_cents, o.admission_currency,
  en.id AS entitlement_id, en.plan
FROM event_occurrences o
JOIN events e ON e.id = o.event_id
JOIN promoters p ON p.id = e.promoter_id
JOIN venues v ON v.id = o.venue_id
JOIN event_revisions r ON r.id = e.published_revision_id AND r.event_id = e.id
JOIN listing_entitlements en ON en.event_id = e.id
WHERE p.status = 'Approved'
  AND r.status = 'Approved'
  AND e.visibility_suppressed_at IS NULL
  AND o.emergency_hidden_at IS NULL
  AND o.status = 'Scheduled'
  AND o.published_revision_id = e.published_revision_id
  AND en.status = 'Active'
  AND (
    (en.plan='Single' AND en.single_occurrence_id = o.id)
    OR
    (en.plan='RecurringThreeMonths'
      AND o.local_date >= en.term_start_local_date
      AND o.local_date < en.term_end_exclusive_local_date)
  );
-- A new checkout activation and promoter revocation should both be reflected immediately.
-- Do not create a stale cached/materialized copy without explicit invalidation.
COMMIT;
