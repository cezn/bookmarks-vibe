#!/bin/bash

HOST=${1:-localhost}
PORT=${2:-8083}

curl -X POST http://$HOST:$PORT/connectors \
  -H "Content-Type: application/json" \
  -d '{
    "name": "debezium-postgres-bookmarks-outbox",
    "config": {
      "connector.class": "io.debezium.connector.postgresql.PostgresConnector",
      "database.hostname": "db",
      "database.port": "5432",
      "database.user": "postgres",
      "database.password": "secret",
      "database.dbname": "bookmarks",
      "topic.prefix": "outbox",
      "table.include.list": "public.outbox",
      "plugin.name": "pgoutput",
      "slot.name": "debezium_outbox_slot",
      "key.converter": "io.confluent.connect.protobuf.ProtobufConverter",
      "key.converter.schema.registry.url": "http://schema-registry:8081",
      "value.converter": "io.debezium.converters.BinaryDataConverter",
      "transforms": "outbox",
      "transforms.outbox.type": "io.debezium.transforms.outbox.EventRouter",
      "transforms.outbox.table.field.event.key": "user_id",
      "transforms.outbox.table.fields.additional.placement": "type:header:type",
      "tracing.span.context.field": "tracingspancontext",
      "tracing.with.context.field.only": false
    }
  }'

