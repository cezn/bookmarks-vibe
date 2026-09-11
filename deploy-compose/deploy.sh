#! /bin/bash

mkdir -p ~/projects
cd ~/projects

if [ ! -d "bookmarks-vibe" ]; then
  gh repo clone cezn/bookmarks-vibe
fi

cd bookmarks-vibe

dotnet tool restore
docker compose -f ./compose.yml -f ./deploy-compose/compose.override.prod.yml --profile infra up -d

function get_container_ip() {
  local service=$1
  docker inspect \
    -f '{{range.NetworkSettings.Networks}}{{.IPAddress}}{{end}}' \
    $(docker ps -q -f name=$service)
}

(
  cd ./src/services/Auth
  dotnet publish -r linux-x64 -t PublishContainer -v d
  ./scripts/migrate-db.sh `get_container_ip db`
)

(
  cd ./src/services/BookmarksApi
  dotnet publish -r linux-x64 -t PublishContainer -v d
  ./scripts/migrate-db.sh `get_container_ip db`
  ./scripts/init-postgres-connector.sh `get_container_ip kafka-connect`
)

(
  cd ./src/services/bookmarks-react-ui
  docker run \
    --rm \
    -v $(pwd):/app/bookmarks-react-ui \
    -w /app/bookmarks-react-ui node:24-slim bash \
    -c "npm i && ./scripts/build.sh"
)

(
  cd ./src/services/StaticAssets
  ./scripts/copy.sh
  dotnet publish -r linux-x64 -t PublishContainer -v d
)

(
  cd ./src/services/Gateway
  ./scripts/generate-cert.sh
  dotnet publish -r linux-x64 -t PublishContainer -v d
)

(
  cd ./src/services/TagsBookmarksSubscriber
  dotnet publish -r linux-x64 -t PublishContainer -v d
)

(
  cd ./src/services/SummarizeApi
  dotnet publish -r linux-x64 -t PublishContainer -v d
)

docker compose -f ./compose.yml -f ./compose.override.prod.yml --profile app up
