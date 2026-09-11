# Running with Aspire

## Prerequisites

1. **.NET 10 SDK**
2. **Aspire CLI**
   ```shell
   dotnet tool install -g Aspire.Cli
   ```
   (or `curl -sSL https://aspire.dev/install.sh | bash`)
3. **Docker** — required, the AppHost starts the Kafka/Postgres/Redis/etc. containers from `compose.yml` via `AddDockerComposeEnvironment`.
4. **Node.js / npm** — required for the React UI (`src/services/bookmarks-react-ui`), which the AppHost runs as a Vite app.
5. **Ollama** — `SummarizeApi` talks to Ollama; make sure it's running (or run `ollama_port_forward` on the host).

## Setup

The `aspire` folder is a **git submodule** (`https://github.com/cezn/aspire.git`) containing the custom `Cezn.Aspire.Hosting.*` packages. A plain `git clone` leaves it empty, which breaks `dotnet restore`/build with `MSB3202: project file ... was not found`. Initialize it first:

```shell
git submodule update --init --recursive
```

Then restore the packages and the local .NET tools (defined in `.config/dotnet-tools.json`, e.g. `grate`, `dotnet-ef`):

```shell
dotnet restore
dotnet tool restore
```

## Run

From the repo root:

```shell
aspire start
```

`aspire start` auto-detects the `AppHost` project and launches the whole distributed app (all .NET services + the Docker Compose containers + the Aspire dashboard).

> Use `aspire start` — **not** `dotnet run` on the AppHost.

You can then open dashboard and use links available on each resource.
Use 'gateway' link to open webapp.
If there are problems, ensure are resources are either started/finished.
After registration, confirmation email is sent to 'mailhog' also available via Aspire dashboard.
You can also use 'auth' resource command on Aspire dashboard to seed user without registration. Credentials will appear in the success model after few seconds.
Note, Kafka related services are the slowest to start, and unfortunately they are needed because api uses protobuf schemas from Schema Registry which itself depends on Kafka for storage. Ugly dependency and I'm planning to loosen it in the future. Otherwise, it would only block tags stats calculation.
If you use remote ssh host/WSL or any kind of remote development tool with VS Code, ports are not automatically forwarded when url is clicked on the dashboard (like they do in the terminal). They must be forwarded manually.

## Run tests

Initially, I was running everything with docker-compose. I've migrated services to Aspire, but tests are still relying on ports from compose files. But if needed, there are bunch of [VS Code tasks](.vscode/tasks.json) to start everything. For example, to run BookmarksApi.Tests, run these tasks (`ctrl+shift+t` in VS Code):

1. Docker Compose Up
2. BookmarksApi: Migrate DB
3. `dotnet run --project  tests/services/BookmarksApi.Tests/`

```mermaid
flowchart LR
  %% Browser
  subgraph UI [ ]
    ReactUi["<b>🖥️ ReactUi</b>"]
  end


  %% Local services
  subgraph Local [ ]
    Gateway["<b>🔗 Gateway</b>"]
    Auth["<b>🔗 Auth</b>"]
    BookmarksApi["<b>📚 BookmarksApi</b>"]
    SummarizeApi["<b>📝 SummarizeApi</b>"]
    NotificationsHub["<b>💻 NotificationsHub</b>"]
    BookmarksCli["<b>💻 BookmarksCli</b>"]
    StaticAssets["<b>💻 StaticAssets</b>"]
    TagsBookmarksSubscriber["<b>🔔 TagsBookmarksSubscriber</b>"]
  end

  %% Kafka containers
  subgraph KafkaContainers [ ]
    Kafka["<b>🦄 Kafka</b>"]
    SchemaRegistry["<b>📦 SchemaRegistry</b>"]
    KafkaConnect["<b>🔌 KafkaConnect</b>"]
    KafkaUi["<b>📊 KafkaUi</b>"]
  end

  %% Other containers
  subgraph InfraContainers [ ]
    Postgres["<b>🗄️ Postgres</b>"]
    Ollama["<b>🤖 Ollama</b>"]
    Redis["<b>🤖 Redis</b>"]
    OtelCollector["<b>📡 OtelCollector</b>"]
    AspireDashboard["<b>📈 AspireDashboard</b>"]
  end

  %% Connections
  ReactUi --> Gateway
  Gateway --> BookmarksApi
  Gateway --> SummarizeApi
  Gateway --> NotificationsHub
  Gateway --> Redis
  Gateway --> Auth
  Gateway --> StaticAssets
  Gateway --> OtelCollector
  Auth --> Redis
  Auth --> Postgres
  SummarizeApi --> Ollama
  SummarizeApi --> BookmarksApi
  BookmarksCli --> BookmarksApi
  BookmarksApi --> Postgres
  BookmarksApi --> Kafka
  BookmarksApi --> SchemaRegistry
  TagsBookmarksSubscriber --> Kafka
  TagsBookmarksSubscriber --> BookmarksApi
  Gateway --> AspireDashboard
  Auth --> AspireDashboard
  BookmarksApi --> AspireDashboard
  SummarizeApi --> AspireDashboard
  NotificationsHub --> AspireDashboard
  BookmarksCli --> AspireDashboard
  StaticAssets --> AspireDashboard
  TagsBookmarksSubscriber --> AspireDashboard
  KafkaConnect --> Postgres
  KafkaConnect --> Kafka
  KafkaConnect --> SchemaRegistry
  KafkaConnect --> AspireDashboard
  KafkaUi --> KafkaConnect
  KafkaUi --> Kafka
  KafkaUi --> SchemaRegistry
  SchemaRegistry --> Kafka
  SchemaRegistry --> AspireDashboard
  OtelCollector --> AspireDashboard


  %% Styles
  %% Style incoming links to AspireDashboard
  linkStyle 18 stroke:#ff8800,stroke-width:2px,stroke-dasharray:5 5
  linkStyle 19 stroke:#ff8800,stroke-width:2px,stroke-dasharray:5 5
  linkStyle 20 stroke:#ff8800,stroke-width:2px,stroke-dasharray:5 5
  linkStyle 21 stroke:#ff8800,stroke-width:2px,stroke-dasharray:5 5
  linkStyle 22 stroke:#ff8800,stroke-width:2px,stroke-dasharray:5 5
  linkStyle 23 stroke:#ff8800,stroke-width:2px,stroke-dasharray:5 5
  linkStyle 24 stroke:#ff8800,stroke-width:2px,stroke-dasharray:5 5
  linkStyle 25 stroke:#ff8800,stroke-width:2px,stroke-dasharray:5 5
  linkStyle 29 stroke:#ff8800,stroke-width:2px,stroke-dasharray:5 5
  linkStyle 34 stroke:#ff8800,stroke-width:2px,stroke-dasharray:5 5
  linkStyle 35 stroke:#ff8800,stroke-width:2px,stroke-dasharray:5 5
  classDef dotnet fill:#2b5797,stroke:#ffffff,color:#ffffff;
  classDef container fill:#f0f0f0,stroke:#cccccc,color:#333333;
  classDef nodejs fill:#8CC84B,stroke:#ffffff,color:#ffffff;
  classDef infra fill:#f9f9f9,stroke:#888888,color:#333333;

  class BookmarksApi,BookmarksCli,SummarizeApi,Gateway,Auth,NotificationsHub,StaticAssets,TagsBookmarksSubscriber dotnet;
  class Postgres,SchemaRegistry,Kafka,KafkaUi,KafkaConnect,OtelCollector container;
  class ReactUi nodejs;
  class Ollama,Redis iaanfra;
```
