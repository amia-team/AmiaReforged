#!/bin/bash
# Restarts the test server
pushd /home/amia/amia_server || exit

docker compose up -d nwserver

popd || exit
