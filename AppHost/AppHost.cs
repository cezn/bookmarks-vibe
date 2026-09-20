using Cezn.Aspire.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);
builder.AddDockerComposeEnvironment("compose1");
builder.HideDefaultResourceUrls();

var jwtKey = builder.AddParameter("jwt-key");
var bookmarksApiKey = builder.AddParameter("bookmarks-api-key");

var pg = builder
    .AddPostgres("pg")
    .WithArgs("-c", "wal_level=logical", "-c", "max_wal_senders=10", "-c", "log_min_duration_statement=100")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(pgAdminBuilder =>
    {
        pgAdminBuilder.WithLifetime(ContainerLifetime.Persistent);
        pgAdminBuilder.WithUrlForEndpoint("http", url => url.DisplayText = "pgAdmin");
    });

// Hardcoded port so that continer is not recreated each time when its env var changes
var kafka = builder.AddKafka("kafka", port: 9092).WithLifetime(ContainerLifetime.Persistent);

var schemaRegistry = builder
    .AddSchemaRegistry("schema-registry")
    .WithKafka(kafka)
    .WithLifetime(ContainerLifetime.Persistent);

var prometheus = builder.AddPrometheus("prometheus");

// The collector forwards metrics to both the Aspire dashboard (OTLP) and Prometheus (remote write).
var otelCollector = builder.AddOtelCollector("otel-collector", prometheus);

var kafkaConnect = builder
    .AddKafkaConnect("kafka-connect")
    .WithKafka(kafka)
    .WithOtel(otelCollector)
    .WithLifetime(ContainerLifetime.Persistent);

var kafkaConsole = builder
    .AddRedpandaConsole("kafka-console")
    .WithKafka(kafka)
    .WithSchemaRegistry(schemaRegistry)
    .WithKafkaConnect(kafkaConnect)
    .WithUrlForEndpoint("http", url => url.DisplayText = "Kafka Console");

// monitoring: kafka-metrics (JMX exporter) -> prometheus -> grafana
var kafkaMetrics = builder.AddKafkaJmxExporter("kafka-metrics").WithKafka(kafka);
prometheus.WithScrapeTarget(kafkaMetrics);
var grafana = builder.AddGrafana("grafana").WithPrometheus(prometheus);
grafana.WithUrlForEndpoint("http", url => url.DisplayText = "Grafana");

var mailhog = builder
    .AddContainer("mailhog", "mailhog/mailhog", "v1.0.1")
    .WithEndpoint(name: "smtp", targetPort: 1025, protocol: System.Net.Sockets.ProtocolType.Tcp)
    .WithHttpEndpoint(name: "ui", targetPort: 8025)
    .WithUrlForEndpoint("ui", url => url.DisplayText = "MailHog");

var redis = builder
    .AddRedis("redis")
    .WithRedisInsight(ri => ri.WithUrlForEndpoint("http", url => url.DisplayText = "Redis Insight"));

var swagger = builder.AddSwaggerUI().WithUrlForEndpoint("http", url => url.DisplayText = "Swagger UI");

// services
var auth = builder.AddAuth("auth", pg, redis, mailhog, swagger, otelCollector);
var bookmarksApi = builder.AddBookmarks(
    "bookmarks-api",
    pg,
    kafkaConnect,
    schemaRegistry,
    jwtKey,
    swagger,
    otelCollector
);
var tagsBookmarksSubscriber = builder.AddTagsBookmarksSubscriber(
    "tags-bookmarks-subscriber",
    kafka,
    schemaRegistry,
    bookmarksApi,
    bookmarksApiKey,
    otelCollector
);
var ui = builder.AddViteApp("ui", "../src/services/bookmarks-react-ui");
var staticAssets = builder.ExecutionContext.IsPublishMode
    ? builder.AddProject<StaticAssets>("static-assets").PublishWithContainerFiles(ui, "wwwroot")
    : null;
var gateway = builder.AddGateway("gateway", redis, jwtKey, bookmarksApi, auth, ui, otelCollector, staticAssets);

ui.WithEnvironment("VITE_PROXY_TARGET", gateway.GetEndpoint("http"));

builder.Build().Run();
