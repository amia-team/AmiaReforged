#!/bin/bash
# Restarts the test server
pushd /home/amia/amia_server || exit

sudo docker-compose up -d nwserver

popd || exit