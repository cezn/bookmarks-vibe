-- Drop the old unique index on name only
DROP INDEX IF EXISTS idx_tags_name;

-- Create a new unique index on (name, user_id) composite
CREATE UNIQUE INDEX IF NOT EXISTS idx_tags_name_user_id ON tags(name, user_id);
