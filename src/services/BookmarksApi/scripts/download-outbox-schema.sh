#! /bin/bash

HOST=${1:-localhost}
PORT=${2:-8081}
SCHEMA_NAME="bookmark.outbox.public.outbox-value"
SCHEMA_REGISTRY_URL="http://$HOST:$PORT"
OUTPUT_FILE="${SCHEMA_NAME}.proto"

curl -s "${SCHEMA_REGISTRY_URL}/subjects/${SCHEMA_NAME}/versions/latest" \
  | jq -r '.schema' > outbox.proto

echo "Downloaded latest schema for ${SCHEMA_NAME} to ${OUTPUT_FILE}"

