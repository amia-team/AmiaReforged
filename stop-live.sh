#!/bin/bash
# Restarts the test server
pushd /home/amia/amia_server || exit
sudo docker-compose stop nwserver
sudo docker-compose rm -f nwserver

popd || exit