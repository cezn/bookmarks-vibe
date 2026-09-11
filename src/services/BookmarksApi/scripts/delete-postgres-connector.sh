#! /bin/bash

HOST=${1:-localhost}
PORT=${2:-8083}

curl -i -X DELETE http://$HOST:$PORT/connectors/debezium-postgres-bookmarks-outbox

# Drop all logical replication slots for the database
DB_NAME="bookmarks"
DB_USER="postgres"

for i in {1..3}; do
  if psql -U "$DB_USER" -d postgres -c "SELECT pg_drop_replication_slot(slot_name) FROM pg_replication_slots WHERE database = '"$DB_NAME"';" 2>/dev/null; then
    break
  else
    sleep $((2**(i-1)))
  fi
done
