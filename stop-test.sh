#!/bin/bash
# Stops the test server
# Uses AMIA_SERVER_DIR environment variable if set, otherwise defaults to /home/amia/amia_server
SERVER_DIR="${AMIA_SERVER_DIR:-/home/amia/amia_server}"
pushd "$SERVER_DIR" || exit
docker compose stop test-server
docker compose rm -f test-server

popd || exit
