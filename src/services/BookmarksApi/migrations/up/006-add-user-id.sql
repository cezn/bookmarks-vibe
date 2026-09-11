-- Add user_id column to bookmarks table
ALTER TABLE bookmarks
ADD COLUMN user_id VARCHAR(255) NOT NULL DEFAULT 'migration-user-id';

-- Add user_id column to tags table
ALTER TABLE tags
ADD COLUMN user_id VARCHAR(255) NOT NULL DEFAULT 'migration-user-id';

-- Create indexes for faster queries by user_id
CREATE INDEX idx_bookmarks_user_id ON bookmarks(user_id);
CREATE INDEX idx_tags_user_id ON tags(user_id);

-- Create composite indexes for common query patterns
CREATE INDEX idx_bookmarks_user_id_created_at ON bookmarks(user_id, created_at DESC);
CREATE INDEX idx_tags_user_id_usage_count ON tags(user_id, usage_count DESC);
