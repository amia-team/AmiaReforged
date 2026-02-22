#!/bin/bash
# Stops the live server
# Uses AMIA_SERVER_DIR environment variable if set, otherwise defaults to /home/amia/amia_server
SERVER_DIR="${AMIA_SERVER_DIR:-/home/amia/amia_server}"
pushd "$SERVER_DIR" || exit
docker compose stop nwserver
docker compose rm -f nwserver

popd || exit
