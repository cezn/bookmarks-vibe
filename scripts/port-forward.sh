#!/bin/bash

PORTS=(
  5432    # postgres
  6379    # redis
  8001    # redis ui
  18888   # aspire-dashboard ui
  4317    # aspire-dashboard grpc
  4318    # otel-collector http
  9092    # kafka broker
  8081    # schema registry
  8083    # kafka connect
  8080    # redpanda console (mapped from 8084)
  3000    # grafana
  1025    # mailhog
)

DEST_HOST="bob.local"
SOCAT_PIDS=()

cleanup() {
  echo "Stopping all socat port forwardings..."
  for pid in "${SOCAT_PIDS[@]}"; do
    kill "$pid" 2>/dev/null
  done
  exit 0
}

trap cleanup SIGINT SIGTERM

for PORT in "${PORTS[@]}"; do
  echo "Forwarding local port $PORT to $DEST_HOST:$PORT"
  socat TCP-LISTEN:$PORT,reuseaddr,fork TCP:$DEST_HOST:$PORT &
  SOCAT_PIDS+=($!)
done

echo "Port forwarding active. Press Ctrl+C to stop."

# Wait indefinitely until interrupted
while true; do
  sleep 1
done
