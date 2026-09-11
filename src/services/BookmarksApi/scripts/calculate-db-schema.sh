#!/bin/bash

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
MIGRATIONS_DIR="$PROJECT_ROOT/migrations/up"
DB_NAME="schema_calc_db"
DB_USER="postgres"
DB_PASSWORD="postgres"
DB_HOST="localhost"
DB_PORT="5432"
CONTAINER_NAME="bookmarks-schema-calc"
POSTGRES_IMAGE="postgres:16.2"
OUTPUT_FILE="${OUTPUT_FILE:-./schema.sql}"

# Flags
KEEP_CONTAINER=false
SKIP_DOCKER=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --keep)
            KEEP_CONTAINER=true
            shift
            ;;
        --skip-docker)
            SKIP_DOCKER=true
            shift
            ;;
        --output)
            OUTPUT_FILE="$2"
            shift 2
            ;;
        --help)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --keep              Keep the Docker container after execution"
            echo "  --skip-docker       Use existing database connection instead of Docker"
            echo "  --output FILE       Output file for schema (default: ./schema.sql)"
            echo "  --help              Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Function to log messages
log() {
    echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

# Check if migrations directory exists
if [ ! -d "$MIGRATIONS_DIR" ]; then
    log_error "Migrations directory not found: $MIGRATIONS_DIR"
    exit 1
fi

# Function to cleanup container
cleanup() {
    if [ "$SKIP_DOCKER" = false ] && [ "$KEEP_CONTAINER" = false ]; then
        log "Cleaning up Docker container..."
        docker stop "$CONTAINER_NAME" 2>/dev/null || true
        docker rm "$CONTAINER_NAME" 2>/dev/null || true
    fi
}

# Set trap to cleanup on exit
trap cleanup EXIT

# Start Docker container if not skipping
if [ "$SKIP_DOCKER" = false ]; then
    log "Starting PostgreSQL container..."

    # Check if container already exists
    if docker ps -a --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
        log "Removing existing container..."
        docker stop "$CONTAINER_NAME" 2>/dev/null || true
        docker rm "$CONTAINER_NAME" 2>/dev/null || true
    fi

    # Start new container
    docker run \
        --name "$CONTAINER_NAME" \
        -e POSTGRES_USER="$DB_USER" \
        -e POSTGRES_PASSWORD="$DB_PASSWORD" \
        -e POSTGRES_DB="$DB_NAME" \
        -p "$DB_PORT:5432" \
        -d \
        "$POSTGRES_IMAGE" \
        > /dev/null

    # Wait for PostgreSQL to be ready
    log "Waiting for PostgreSQL to be ready..."
    for i in {1..30}; do
        if docker exec "$CONTAINER_NAME" pg_isready -U "$DB_USER" > /dev/null 2>&1; then
            log "PostgreSQL is ready"
            break
        fi
        if [ $i -eq 30 ]; then
            log_error "PostgreSQL did not start in time"
            exit 1
        fi
        sleep 1
    done
fi

# Create psql command
PSQL_CMD="psql -h $DB_HOST -U $DB_USER -d $DB_NAME -p $DB_PORT"
export PGPASSWORD="$DB_PASSWORD"

# Get list of migration files sorted
MIGRATION_FILES=($(ls -1 "$MIGRATIONS_DIR"/*.sql | sort -V))

if [ ${#MIGRATION_FILES[@]} -eq 0 ]; then
    log_error "No migration files found in $MIGRATIONS_DIR"
    exit 1
fi

log "Found ${#MIGRATION_FILES[@]} migration files"

# Execute migrations
log "Executing migrations..."
for migration_file in "${MIGRATION_FILES[@]}"; do
    migration_name=$(basename "$migration_file")
    log "Executing migration: $migration_name"

    if ! $PSQL_CMD -f "$migration_file" > /dev/null 2>&1; then
        log_error "Failed to execute migration: $migration_name"
        exit 1
    fi
done

log "Migrations completed successfully"

# Export schema
log "Exporting schema to $OUTPUT_FILE..."
$PSQL_CMD -t --no-password -c "
SELECT
    'CREATE TABLE ' || schemaname || '.' || tablename || ' (' || E'\n' ||
    string_agg('  ' || column_name || ' ' || data_type ||
        CASE WHEN is_nullable = 'NO' THEN ' NOT NULL' ELSE '' END ||
        CASE WHEN column_default IS NOT NULL THEN ' DEFAULT ' || column_default ELSE '' END,
        ',' || E'\n')
    || E'\n);'
FROM
    information_schema.columns
    JOIN information_schema.tables USING (table_catalog, table_schema, table_name)
WHERE
    table_schema = 'public'
    AND table_name NOT LIKE 'pg_%'
GROUP BY
    schemaname,
    tablename
ORDER BY
    tablename
" > "$OUTPUT_FILE.tmp" 2>/dev/null || true

# Add index information
$PSQL_CMD -t --no-password -c "
SELECT
    indexdef || ';'
FROM
    pg_indexes
WHERE
    schemaname = 'public'
ORDER BY
    tablename,
    indexname
" >> "$OUTPUT_FILE.tmp" 2>/dev/null || true

# Add function definitions
$PSQL_CMD -t --no-password -c "
SELECT
    pg_get_functiondef(pg_proc.oid) || ';'
FROM
    pg_proc
    JOIN pg_namespace ON pg_proc.pronamespace = pg_namespace.oid
WHERE
    pg_namespace.nspname = 'public'
ORDER BY
    pg_proc.proname
" >> "$OUTPUT_FILE.tmp" 2>/dev/null || true

# Clean and format output
if [ -f "$OUTPUT_FILE.tmp" ]; then
    # Remove empty lines and clean up
    sed '/^[[:space:]]*$/d' "$OUTPUT_FILE.tmp" > "$OUTPUT_FILE"
    rm "$OUTPUT_FILE.tmp"

    # Use pg_dump for cleaner schema export
    log "Using pg_dump for complete schema export..."
    $PSQL_CMD -c "" > /dev/null 2>&1  # Test connection

    pg_dump -h "$DB_HOST" -U "$DB_USER" -d "$DB_NAME" -p "$DB_PORT" \
        --schema-only \
        --no-owner \
        --no-acl \
        > "$OUTPUT_FILE" 2>/dev/null || true

    # Remove comments from output
    sed -i '/^--/d; s/ *--.*$//g' "$OUTPUT_FILE"
fi

log "Schema exported to: $(cd "$(dirname "$OUTPUT_FILE")" && pwd)/$(basename "$OUTPUT_FILE")"

# Display summary
log "Schema Summary:"
$PSQL_CMD -c "
SELECT
    t.table_name as tablename,
    count(*) as column_count
FROM
    information_schema.columns c
    JOIN information_schema.tables t ON (c.table_catalog = t.table_catalog AND c.table_schema = t.table_schema AND c.table_name = t.table_name)
WHERE
    t.table_schema = 'public'
    AND t.table_name NOT LIKE 'pg_%'
GROUP BY
    t.table_name
ORDER BY
    t.table_name
" 2>/dev/null || true

log "Schema calculation completed successfully!"

# Display next steps
if [ "$KEEP_CONTAINER" = true ] && [ "$SKIP_DOCKER" = false ]; then
    log_warning "Container is still running. To stop it, run:"
    echo "  docker stop $CONTAINER_NAME && docker rm $CONTAINER_NAME"
fi
