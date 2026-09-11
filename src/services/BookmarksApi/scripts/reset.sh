#! /bin/bash


./scripts/delete-postgres-connector.sh
./scripts/recreate-db.sh
./scripts/init-postgres-connector.sh
echo "Starting database reset process..."
./scripts/delete-postgres-connector.sh && echo "✓ Deleted postgres connector" || echo "✗ Failed to delete postgres connector"
./scripts/recreate-db.sh && echo "✓ Recreated database" || echo "✗ Failed to recreate database"
./scripts/init-postgres-connector.sh && echo "✓ Initialized postgres connector" || echo "✗ Failed to initialize postgres connector"
./scripts/migrate-db.sh && echo "✓ Migrated database" || echo "✗ Failed to migrate database"
echo "Database reset process completed!"
