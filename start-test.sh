#!/bin/bash
# Restarts the test server
pushd /home/amia/amia_server || exit
docker-compose up -d test-server
popd || exit