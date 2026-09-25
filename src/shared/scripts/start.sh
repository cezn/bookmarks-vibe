#!/bin/bash

if [ $# -lt 2 ]; then
    echo "Usage: $0 <service_name> <instance_id> [-- <dotnet_args>]"
    exit 1
fi

SERVICE_NAME=$1
INSTANCE_ID=$2
shift 2

if [ "$1" = "--" ]; then
    shift
    DOTNET_ARGS="$@"
else
    DOTNET_ARGS=""
fi

# OTEL Exporter
export OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:5088" # 5088 grpc | 5089 http/protobuf
export OTEL_EXPORTER_OTLP_PROTOCOL="grpc" # http/protobuf | http/json | grpc`
export OTEL_EXPORTER_OTLP_TIMEOUT="10000" # ms
export OTEL_EXPORTER_OTLP_COMPRESSION="none" # gzip
export OTEL_EXPORTER_OTLP_INSECURE="true"

# General SDK Configuration
export OTEL_SDK_DISABLED="false"
export OTEL_RESOURCE_ATTRIBUTES="service.version=1.0,service.instance.id=${INSTANCE_ID},deployment.environment=production"
export OTEL_SERVICE_NAME="${SERVICE_NAME}"
export OTEL_TRACES_EXPORTER="otlp"
export OTEL_METRICS_EXPORTER="otlp"
export OTEL_LOGS_EXPORTER="otlp"
export OTEL_TRACES_SAMPLER="always_on"
export OTEL_TRACES_SAMPLER_ARG=""
export OTEL_LOG_LEVEL="warn"
export OTEL_PROPAGATORS="tracecontext,baggage"

# Batch Span Processor
export OTEL_BSP_SCHEDULE_DELAY="2000"
export OTEL_BSP_EXPORT_TIMEOUT="30000"
export OTEL_BSP_MAX_QUEUE_SIZE="2048"
export OTEL_BSP_MAX_EXPORT_BATCH_SIZE="512"

# Batch LogRecord Processor
export OTEL_BLRP_SCHEDULE_DELAY="2000"
export OTEL_BLRP_EXPORT_TIMEOUT="30000"
export OTEL_BLRP_MAX_QUEUE_SIZE="2048"
export OTEL_BLRP_MAX_EXPORT_BATCH_SIZE="512"

# Periodic exporting MetricReader
export OTEL_METRIC_EXPORT_INTERVAL="5000"
export OTEL_METRIC_EXPORT_TIMEOUT="3000"

dotnet run $DOTNET_ARGS
