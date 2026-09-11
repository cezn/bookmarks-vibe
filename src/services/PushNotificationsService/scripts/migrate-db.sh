#! /bin/bash

dotnet grate \
  -a 'host=localhost;database=postgres;user id=postgres;password=secret;' \
  -c 'host=localhost;database=bookmarks;user id=postgres;password=secret;' \
  --files ./migrations \
  --dt postgresql \
  --env local \
  --create \
  -ni
