#!/bin/bash
# Restarts the test server
pushd /home/amia/dev_server || exit
sudo docker-compose stop devserver
sudo docker-compose rm -f devserver

popd || exit