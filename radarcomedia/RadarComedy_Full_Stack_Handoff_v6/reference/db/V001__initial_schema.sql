-- RadarComedy v5: PostgreSQL 17 + PostGIS (initial database schema)
-- Apply via migration tooling ONCE to an empty application database.
-- Amounts are USD minor units; timestamps are UTC timestamptz; local show dates are DATE.
-- This SQL is the canonical audit/reference schema. The EF Core DbContext is a typed mapping.
-- For production use a single migration system, not both manual psql and EF migrations.
BEGIN;
CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS btree_gist;

CREATE TABLE app_users (
 id uuid PRIMARY KEY, email_normalized varchar(320) NOT NULL UNIQUE,
 email_verified_at timestamptz, role varchar(32) NOT NULL DEFAULT 'Fan'
   CHECK (role IN ('Fan','Promoter','Admin')),
 created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE promoters (
 id uuid PRIMARY KEY, owner_user_id uuid NOT NULL UNIQUE REFERENCES app_users(id),
 display_name varchar(180) NOT NULL, contact_name varchar(180) NOT NULL,
 public_profile_url text NOT NULL CHECK (public_profile_url ~* '^https://'),
 city_code varchar(32) NOT NULL CHECK (city_code IN ('SAN_DIEGO','TIJUANA')),
 status varchar(32) NOT NULL DEFAULT 'PendingReview' CHECK (status IN ('PendingReview','Approved','Rejected','Suspended')),
 reviewed_by uuid REFERENCES app_users(id), reviewed_at timestamptz, review_reason text,
 created_at timestamptz NOT NULL DEFAULT now(), version bigint NOT NULL DEFAULT 1
);

CREATE TABLE venues (
 id uuid PRIMARY KEY, name varchar(200) NOT NULL, street_address text NOT NULL,
 city varchar(120) NOT NULL, region varchar(120) NOT NULL,
 postal_code varchar(24), country_code char(2) NOT NULL CHECK (country_code IN ('US','MX')),
 city_code varchar(32) NOT NULL CHECK (city_code IN ('SAN_DIEGO','TIJUANA')),
 time_zone_id varchar(100) NOT NULL, location geography(Point,4326) NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE events (
 id uuid PRIMARY KEY, promoter_id uuid NOT NULL REFERENCES promoters(id),
 slug varchar(160) NOT NULL UNIQUE,
 category varchar(48) NOT NULL CHECK (category IN ('StandUp','OpenMic','Improv','Sketch','ComedyTheater','Festival','OtherLiveComedy')),
 format varchar(32) NOT NULL CHECK (format IN ('Single','Recurring')),
 published_revision_id uuid,
 visibility_suppressed_at timestamptz, suppression_reason text,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
 version bigint NOT NULL DEFAULT 1
);

CREATE TABLE event_revisions (
 id uuid PRIMARY KEY, event_id uuid NOT NULL REFERENCES events(id),
 revision_number integer NOT NULL CHECK (revision_number >= 1),
 based_on_revision_id uuid REFERENCES event_revisions(id),
 snapshot_json jsonb NOT NULL CHECK (jsonb_typeof(snapshot_json) = 'object'),
 status varchar(32) NOT NULL CHECK (status IN ('Draft','Submitted','ChangesRequested','Approved','Rejected','Superseded')),
 submitted_at timestamptz, reviewed_by uuid REFERENCES app_users(id), reviewed_at timestamptz, review_reason text,
 created_at timestamptz NOT NULL DEFAULT now(),
 UNIQUE(event_id, revision_number), UNIQUE(event_id,id)
);

ALTER TABLE events ADD CONSTRAINT events_published_revision_fk
 FOREIGN KEY (id,published_revision_id) REFERENCES event_revisions(event_id,id) DEFERRABLE INITIALLY IMMEDIATE;

CREATE TABLE recurrence_schedules (
 event_id uuid PRIMARY KEY REFERENCES events(id) ON DELETE CASCADE,
 venue_id uuid NOT NULL REFERENCES venues(id),
 frequency varchar(20) NOT NULL CHECK (frequency IN ('Daily','Weekly','Monthly')),
 interval_count integer NOT NULL DEFAULT 1 CHECK (interval_count >= 1 AND interval_count <= 31),
 days_of_week_mask integer NOT NULL DEFAULT 0 CHECK (days_of_week_mask BETWEEN 0 AND 127),
 day_of_month integer CHECK (day_of_month BETWEEN 1 AND 31),
 local_start_time time NOT NULL, duration_minutes integer NOT NULL CHECK (duration_minutes BETWEEN 1 AND 1440),
 time_zone_id varchar(100) NOT NULL,
 -- Implementation validates this schedule emits <= 1 performance per local calendar day.
 published_revision_id uuid REFERENCES event_revisions(id)
);

CREATE TABLE event_occurrences (
 id uuid PRIMARY KEY, event_id uuid NOT NULL REFERENCES events(id),
 published_revision_id uuid NOT NULL REFERENCES event_revisions(id),
 venue_id uuid NOT NULL REFERENCES venues(id),
 local_date date NOT NULL, local_start_time time NOT NULL,
 starts_at_utc timestamptz NOT NULL, ends_at_utc timestamptz NOT NULL,
 time_zone_id varchar(100) NOT NULL,
 status varchar(24) NOT NULL DEFAULT 'Scheduled' CHECK (status IN ('Scheduled','Canceled','Postponed','Completed')),
 emergency_hidden_at timestamptz,
 ticket_url text NOT NULL CHECK (ticket_url ~* '^https://'),
 admission_price_cents integer CHECK (admission_price_cents >= 0),
 admission_currency char(3) CHECK (admission_currency IN ('USD','MXN')),
 created_at timestamptz NOT NULL DEFAULT now(),
 CHECK (ends_at_utc > starts_at_utc),
 CHECK ((admission_price_cents IS NULL) = (admission_currency IS NULL))
);
-- For a fixed series, no two active performances on the same local date.
CREATE UNIQUE INDEX uq_event_one_scheduled_per_local_date
 ON event_occurrences(event_id,local_date) WHERE status='Scheduled';

CREATE TABLE listing_entitlements (
 id uuid PRIMARY KEY, event_id uuid NOT NULL REFERENCES events(id),
 plan varchar(32) NOT NULL CHECK (plan IN ('Single','RecurringThreeMonths')),
 price_cents integer NOT NULL,
 currency char(3) NOT NULL DEFAULT 'USD' CHECK (currency='USD'),
 status varchar(32) NOT NULL DEFAULT 'PendingPayment'
   CHECK (status IN ('PendingPayment','Active','Suspended','Expired','Revoked')),
 single_occurrence_id uuid REFERENCES event_occurrences(id),
 term_start_local_date date, term_end_exclusive_local_date date,
 activated_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(),
 CHECK ((plan='Single' AND price_cents=200 AND single_occurrence_id IS NOT NULL AND term_start_local_date IS NULL AND term_end_exclusive_local_date IS NULL)
     OR (plan='RecurringThreeMonths' AND price_cents=500 AND single_occurrence_id IS NULL AND term_start_local_date IS NOT NULL AND term_end_exclusive_local_date IS NOT NULL AND term_end_exclusive_local_date > term_start_local_date))
);

-- Prevent double public coverage of one recurring series from overlapping paid terms.
-- Service must reconcile attempted double payments if two Stripe sessions raced.
ALTER TABLE listing_entitlements ADD CONSTRAINT exclude_overlapping_active_recurring_terms
 EXCLUDE USING gist (
   event_id WITH =,
   daterange(term_start_local_date,term_end_exclusive_local_date,'[)') WITH &&
 ) WHERE (plan='RecurringThreeMonths' AND status='Active');

CREATE TABLE checkout_orders (
 id uuid PRIMARY KEY, entitlement_id uuid NOT NULL REFERENCES listing_entitlements(id),
 amount_cents integer NOT NULL CHECK(amount_cents IN (200,500)),
 tax_included_cents integer CHECK (tax_included_cents >= 0),
 currency char(3) NOT NULL DEFAULT 'USD' CHECK(currency='USD'),
 status varchar(32) NOT NULL DEFAULT 'Created'
   CHECK (status IN ('Created','CheckoutOpen','Paid','Failed','Canceled','Refunded','Disputed')),
 stripe_checkout_session_id varchar(255) UNIQUE,
 stripe_payment_intent_id varchar(255) UNIQUE,
 paid_at timestamptz, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX uq_one_paid_order_per_entitlement ON checkout_orders(entitlement_id) WHERE status='Paid';

CREATE TABLE stripe_webhook_inbox (
 stripe_event_id varchar(255) PRIMARY KEY, event_type varchar(128) NOT NULL,
 payload_json jsonb NOT NULL, status varchar(24) NOT NULL DEFAULT 'Received'
   CHECK (status IN ('Received','Processed','Failed')),
 received_at timestamptz NOT NULL DEFAULT now(), processed_at timestamptz, error_summary text
);

CREATE TABLE transfer_requests (
 id uuid PRIMARY KEY, entitlement_id uuid NOT NULL REFERENCES listing_entitlements(id),
 original_occurrence_id uuid NOT NULL REFERENCES event_occurrences(id),
 proposed_revision_id uuid NOT NULL REFERENCES event_revisions(id),
 proposed_local_date date NOT NULL, reason text,
 status varchar(24) NOT NULL DEFAULT 'Requested'
   CHECK (status IN ('Requested','Approved','Rejected','Applied')),
 reviewer_id uuid REFERENCES app_users(id), reviewed_at timestamptz,
 requested_at timestamptz NOT NULL DEFAULT now(), applied_at timestamptz
);

CREATE TABLE emergency_hides (
 id uuid PRIMARY KEY, occurrence_id uuid NOT NULL REFERENCES event_occurrences(id),
 promoter_user_id uuid NOT NULL REFERENCES app_users(id),
 reason text NOT NULL, submitted_at timestamptz NOT NULL DEFAULT now(),
 reviewed_by uuid REFERENCES app_users(id), reviewed_at timestamptz,
 review_status varchar(24) NOT NULL DEFAULT 'Pending' CHECK (review_status IN ('Pending','Confirmed','Dismissed'))
);

CREATE TABLE occurrence_exceptions (
 id uuid PRIMARY KEY, event_id uuid NOT NULL REFERENCES events(id),
 original_local_date date NOT NULL,
 kind varchar(24) NOT NULL CHECK (kind IN ('Skip','Move')),
 replacement_local_date date,
 approved_revision_id uuid NOT NULL REFERENCES event_revisions(id),
 CHECK ((kind='Skip' AND replacement_local_date IS NULL) OR (kind='Move' AND replacement_local_date IS NOT NULL)),
 UNIQUE(event_id,original_local_date,approved_revision_id)
);

CREATE TABLE comedians (
 id uuid PRIMARY KEY, stage_name varchar(180) NOT NULL, slug varchar(180) NOT NULL UNIQUE,
 bio text, photo_key text
);
CREATE TABLE event_performers (
 occurrence_id uuid NOT NULL REFERENCES event_occurrences(id) ON DELETE CASCADE,
 comedian_id uuid NOT NULL REFERENCES comedians(id), billing_order smallint NOT NULL DEFAULT 0,
 PRIMARY KEY (occurrence_id,comedian_id)
);

CREATE TABLE moderation_actions (
 id uuid PRIMARY KEY, subject_type varchar(48) NOT NULL, subject_id uuid NOT NULL,
 action varchar(64) NOT NULL, reviewer_id uuid NOT NULL REFERENCES app_users(id),
 reason text, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE event_reports (
 id uuid PRIMARY KEY, event_id uuid NOT NULL REFERENCES events(id),
 occurrence_id uuid REFERENCES event_occurrences(id),
 reason varchar(64) NOT NULL, details text,
 created_at timestamptz NOT NULL DEFAULT now(), resolved_at timestamptz
);

CREATE TABLE newsletter_subscribers (
 id uuid PRIMARY KEY, email_normalized varchar(320) NOT NULL,
 city_code varchar(32) NOT NULL CHECK (city_code IN ('SAN_DIEGO','TIJUANA')),
 language char(2) NOT NULL CHECK (language IN ('en','es')),
 consent_at timestamptz NOT NULL, confirmed_at timestamptz,
 unsubscribed_at timestamptz, confirmation_token_hash text,
 created_at timestamptz NOT NULL DEFAULT now(),
 UNIQUE(email_normalized,city_code,language)
);

CREATE TABLE import_sources (
 id uuid PRIMARY KEY, name varchar(100) NOT NULL UNIQUE, terms_url text NOT NULL,
 permitted_internal_use boolean NOT NULL DEFAULT false, permission_notes text,
 checked_at timestamptz NOT NULL, is_enabled boolean NOT NULL DEFAULT false
);
CREATE TABLE import_candidates (
 id uuid PRIMARY KEY, source_id uuid NOT NULL REFERENCES import_sources(id),
 external_id text NOT NULL, source_url text NOT NULL,
 permitted_metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 promoter_claim_id uuid REFERENCES promoters(id), fetched_at timestamptz NOT NULL,
 UNIQUE(source_id,external_id)
);

CREATE INDEX ix_venues_geo ON venues USING gist(location);
CREATE INDEX ix_occurrences_time ON event_occurrences(starts_at_utc);
CREATE INDEX ix_occurrences_event_time ON event_occurrences(event_id,starts_at_utc);
CREATE INDEX ix_entitlements_event_status ON listing_entitlements(event_id,status);
CREATE INDEX ix_revisions_event_status ON event_revisions(event_id,status);
CREATE INDEX ix_orders_entitlement ON checkout_orders(entitlement_id);
CREATE INDEX ix_promoters_status ON promoters(status);
CREATE INDEX ix_newsletter_city_language ON newsletter_subscribers(city_code,language) WHERE unsubscribed_at IS NULL;
CREATE INDEX ix_import_candidates_source ON import_candidates(source_id);
COMMIT;
