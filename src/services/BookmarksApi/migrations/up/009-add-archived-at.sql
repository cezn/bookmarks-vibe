-- Add archived_at column to bookmarks table for soft-delete archive functionality
ALTER TABLE bookmarks ADD COLUMN archived_at TIMESTAMPTZ NULL;

-- Create index on archived_at for efficient filtering of active bookmarks
CREATE INDEX idx_bookmarks_archived_at ON bookmarks(archived_at);

-- Create partial index for efficient queries of non-archived bookmarks
CREATE INDEX idx_bookmarks_active ON bookmarks(id) WHERE archived_at IS NULL;
