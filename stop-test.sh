#!/bin/bash
# Restarts the test server
pushd /home/amia/amia_server || exit
sudo docker-compose stop test-server
sudo docker-compose rm -f test-server

popd || exit