-- Create an immutable wrapper function
CREATE OR REPLACE FUNCTION immutable_array_to_string(text[], text)
RETURNS text
LANGUAGE sql
IMMUTABLE
AS $$
    SELECT array_to_string($1, $2);
$$;

ALTER TABLE bookmarks ADD COLUMN search_vector tsvector GENERATED ALWAYS AS (
    to_tsvector('english', coalesce(title, '') || ' ' || coalesce(url, '') || ' ' || immutable_array_to_string(tags, ' '))
) STORED;

CREATE INDEX CONCURRENTLY idx_bookmarks_search_vector ON bookmarks USING gin (search_vector);

-- For tags table, add tsvector column
ALTER TABLE tags ADD COLUMN search_vector tsvector GENERATED ALWAYS AS (
    to_tsvector('english', coalesce(name, ''))
) STORED;

CREATE INDEX CONCURRENTLY idx_tags_search_vector ON tags USING gin (search_vector);
