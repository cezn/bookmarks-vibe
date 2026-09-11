#! /bin/bash
DB_NAME="bookmarks"
DB_USER="postgres"

dropdb -U "$DB_USER" --force "$DB_NAME"
createdb -U "$DB_USER" "$DB_NAME"
