#!/bin/bash
# Restarts the test server
# Uses AMIA_SERVER_DIR environment variable if set, otherwise defaults to /home/amia/amia_server
SERVER_DIR="${AMIA_SERVER_DIR:-/home/amia/amia_server}"
pushd "$SERVER_DIR" || exit
docker compose up -d test-server
popd || exit
