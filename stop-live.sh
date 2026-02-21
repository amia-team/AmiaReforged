#!/bin/bash
# Restarts the test server
pushd /home/amia/amia_server || exit
docker compose stop nwserver
docker compose rm -f nwserver

popd || exit
