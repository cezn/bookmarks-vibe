#!/usr/bin/env bash
set -euo pipefail

# Extract connection details from Aspire's PostgreSQL resource description
OUTPUT=$(aspire describe pg --format json)

HOST=$(echo "$OUTPUT" | jq -r '.resources[0].urls[0].url' | sed 's|tcp://||;s|:.*||')
PORT=$(echo "$OUTPUT" | jq -r '.resources[0].urls[0].url' | sed 's|.*:||')
USER=$(echo "$OUTPUT" | jq -r '.resources[0].environment.POSTGRES_USER')
PASSWORD=$(echo "$OUTPUT" | jq -r '.resources[0].environment.POSTGRES_PASSWORD')

if [[ -z "$HOST" || -z "$PORT" || -z "$USER" || -z "$PASSWORD" ]]; then
  echo "Error: failed to extract connection details from Aspire." >&2
  exit 1
fi

export PGPASSWORD="$PASSWORD"

exec psql -h "$HOST" -p "$PORT" -U "$USER" "$@"

