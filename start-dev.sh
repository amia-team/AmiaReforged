#!/bin/bash
# Restarts the dev server
# Uses AMIA_SERVER_DIR environment variable if set, otherwise defaults to /storage/amiadev
SERVER_DIR="${DEV_SERVER_BASE:-/storage/amiadev}"
pushd "$SERVER_DIR" || exit
docker compose up -d dev-server
popd || exit
