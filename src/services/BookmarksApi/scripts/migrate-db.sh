#! /bin/bash

HOST=${1:-localhost}
PORT=${2:-5432}

dotnet grate \
  -a "host=$HOST;port=$PORT;database=postgres;user id=postgres;password=secret;" \
  -c "host=$HOST;port=$PORT;database=bookmarksdb;user id=postgres;password=secret;" \
  --files ./migrations \
  --dt postgresql \
  --env local \
  --create \
  -ni
