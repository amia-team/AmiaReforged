#!/bin/bash
# Stops the dev server
# Uses AMIA_SERVER_DIR environment variable if set, otherwise defaults to /storage/amiadev
SERVER_DIR="${AMIA_SERVER_DIR:-/storage/amiadev}"
pushd "$SERVER_DIR" || exit
docker compose stop dev-server
docker compose rm -f dev-server

popd || exit
