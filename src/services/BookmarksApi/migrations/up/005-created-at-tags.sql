ALTER TABLE tags ADD COLUMN created_at timestamptz(0) NOT NULL DEFAULT now();
