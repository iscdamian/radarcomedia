-- Comedian public-profile MVP extension. Apply after V001 and V002 using one migration owner.
BEGIN;
ALTER TABLE comedians
  ADD COLUMN home_city varchar(180),
  ADD COLUMN country_code char(2),
  ADD COLUMN primary_language varchar(16),
  ADD COLUMN instagram_url text,
  ADD COLUMN tik_tok_url text,
  ADD COLUMN website_url text,
  ADD COLUMN profile_status varchar(16) NOT NULL DEFAULT 'Draft',
  ADD COLUMN approved_by uuid REFERENCES app_users(id),
  ADD COLUMN approved_at timestamptz,
  ADD COLUMN updated_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE comedians
  ADD CONSTRAINT comedian_profile_status_valid CHECK (profile_status IN ('Draft','Published','Hidden')),
  ADD CONSTRAINT comedian_profile_approval_valid CHECK (profile_status <> 'Published' OR (approved_by IS NOT NULL AND approved_at IS NOT NULL)),
  ADD CONSTRAINT comedian_country_valid CHECK (country_code IS NULL OR country_code ~ '^[A-Z]{2}$'),
  ADD CONSTRAINT comedian_instagram_url_valid CHECK (instagram_url IS NULL OR instagram_url ~* '^https://'),
  ADD CONSTRAINT comedian_tik_tok_url_valid CHECK (tik_tok_url IS NULL OR tik_tok_url ~* '^https://'),
  ADD CONSTRAINT comedian_website_url_valid CHECK (website_url IS NULL OR website_url ~* '^https://');
CREATE INDEX comedian_published_name_idx ON comedians (stage_name) WHERE profile_status = 'Published';
CREATE INDEX event_performers_comedian_id_idx ON event_performers (comedian_id);
COMMIT;
