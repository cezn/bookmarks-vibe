# BookmarksApi

The BookmarksApi service provides the core bookmark and tag management functionality for the application.

## Development

### Running Tests

After making any changes to the API, you should run the tests to verify everything still works:

```bash
dotnet run --project ./tests/services/BookmarksApi.Tests/
```

### Prerequisites

Before running tests, ensure that PostgreSQL is running via Docker Compose:

```bash
docker-compose -f ./compose.yml --profile infra up
```

This will start all required infrastructure services including PostgreSQL, Kafka, Redis, Schema Registry, and Ollama.

### Database Migrations

To apply database migrations:

```bash
cd ./src/services/BookmarksApi && ./scripts/migrate-db.sh
```

## Architecture

- **Data Access**: Uses Npgsql directly with separate read/write connection strings
- **Event Publishing**: Uses the outbox pattern for reliable event publishing
- **Idempotency**: Supports idempotent operations to handle duplicate requests
- **Feature Flags**: Includes a feature flag system for gradual rollouts

## Configuration

Configuration is managed through `appsettings.json` and environment-specific files like `appsettings.Development.json`.

## API Documentation

API documentation is available through Swagger UI when running the service.
