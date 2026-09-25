using Cezn.Aspire.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);
builder.AddDockerComposeEnvironment("compose1");
builder.HideDefaultResourceUrls();

var jwtKey = builder.AddParameter("jwt-key");
var bookmarksApiKey = builder.AddParameter("bookmarks-api-key");
var pgPassword = builder.AddParameter("pg-password", "secret");

var pg = builder
    .AddPostgres("pg", port: 5432)
    .WithPassword(pgPassword)
    .WithArgs("-c", "wal_level=logical", "-c", "max_wal_senders=10", "-c", "log_min_duration_statement=100")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(pgAdminBuilder =>
    {
        pgAdminBuilder.WithLifetime(ContainerLifetime.Persistent);
        pgAdminBuilder.WithHostPort(5050);
        pgAdminBuilder.WithUrlForEndpoint("http", url => url.DisplayText = "pgAdmin");
    });

// Hardcoded port so that continer is not recreated each time when its env var changes
var kafka = builder.AddKafka("kafka", port: 9092).WithLifetime(ContainerLifetime.Persistent);

var schemaRegistry = builder
    .AddSchemaRegistry("schema-registry")
    .WithKafka(kafka)
    .WithEndpoint("http", e => e.Port = 5081)
    .WithLifetime(ContainerLifetime.Persistent);

var prometheus = builder.AddPrometheus("prometheus").WithEndpoint("http", e => e.Port = 9090);

// The collector forwards metrics to both the Aspire dashboard (OTLP) and Prometheus (remote write).
var otelCollector = builder
    .AddOtelCollector("otel-collector", prometheus)
    .WithEndpoint("grpc", e => e.Port = 5088)
    .WithEndpoint("http", e => e.Port = 5089)
    .WithEndpoint("fluentforward", e => e.Port = 5090);

var kafkaConnect = builder
    .AddKafkaConnect("kafka-connect")
    .WithKafka(kafka)
    .WithOtel(otelCollector)
    .WithEndpoint("http", e => e.Port = 5080)
    .WithLifetime(ContainerLifetime.Persistent);

var kafkaConsole = builder
    .AddRedpandaConsole("kafka-console")
    .WithKafka(kafka)
    .WithSchemaRegistry(schemaRegistry)
    .WithKafkaConnect(kafkaConnect)
    .WithEndpoint("http", e => e.Port = 5082)
    .WithUrlForEndpoint("http", url => url.DisplayText = "Kafka Console");

// monitoring: kafka-metrics (JMX exporter) -> prometheus -> grafana
var kafkaMetrics = builder
    .AddKafkaJmxExporter("kafka-metrics")
    .WithKafka(kafka)
    .WithEndpoint("metrics", e => e.Port = 5084);
prometheus.WithScrapeTarget(kafkaMetrics);
var grafana = builder.AddGrafana("grafana").WithPrometheus(prometheus).WithEndpoint("http", e => e.Port = 3000);
grafana.WithUrlForEndpoint("http", url => url.DisplayText = "Grafana");

var mailhog = builder
    .AddContainer("mailhog", "mailhog/mailhog", "v1.0.1")
    .WithEndpoint(name: "smtp", port: 5086, targetPort: 1025, protocol: System.Net.Sockets.ProtocolType.Tcp)
    .WithHttpEndpoint(name: "ui", port: 5085, targetPort: 8025)
    .WithUrlForEndpoint("ui", url => url.DisplayText = "MailHog");

var redis = builder
    .AddRedis("redis", port: 6379)
    .WithRedisInsight(ri =>
    {
        ri.WithHostPort(5540);
        ri.WithUrlForEndpoint("http", url => url.DisplayText = "Redis Insight");
    });

var swagger = builder
    .AddSwaggerUI()
    .WithEndpoint("http", e => e.Port = 5087)
    .WithUrlForEndpoint("http", url => url.DisplayText = "Swagger UI");

// Elasticsearch + Kibana are opt-in: pass --elastic (or set elastic=true) to start them.
if (builder.Configuration["elastic"] == "true")
{
    var elasticsearch = builder
        .AddElasticsearch("elasticsearch")
        .WithEndpoint("http", e => e.Port = 9200)
        .WithEndpoint("transport", e => e.Port = 5091);
    var kibana = builder.AddKibana("kibana", elasticsearch).WithEndpoint("http", e => e.Port = 5601);
}

// services
var auth = builder.AddAuth("auth", pg, redis, mailhog, swagger, otelCollector).WithEndpoint("http", e => e.Port = 5006);
var bookmarksApi = builder
    .AddBookmarks("bookmarks-api", pg, kafkaConnect, schemaRegistry, jwtKey, swagger, otelCollector)
    .WithEndpoint("http", e => e.Port = 5002);
var tagsBookmarksSubscriber = builder.AddTagsBookmarksSubscriber(
    "tags-bookmarks-subscriber",
    kafka,
    schemaRegistry,
    bookmarksApi,
    bookmarksApiKey,
    otelCollector
);

// Port matches the dev server port in vite.config.ts so the host port is stable.
var ui = builder.AddViteApp("ui", "../src/services/bookmarks-react-ui").WithEndpoint("http", e => e.Port = 5004);
var staticAssets = builder.ExecutionContext.IsPublishMode
    ? builder.AddProject<StaticAssets>("static-assets").PublishWithContainerFiles(ui, "wwwroot")
    : null;
var gateway = builder
    .AddGateway("gateway", redis, jwtKey, bookmarksApi, auth, ui, otelCollector, staticAssets)
    .WithEndpoint("http", e => e.Port = 5005)
    .WithEndpoint("https", e => e.Port = 7215);

ui.WithEnvironment("VITE_PROXY_TARGET", gateway.GetEndpoint("http"));

builder.Build().Run();
