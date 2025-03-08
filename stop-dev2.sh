#!/bin/bash
# Restarts the test server
pushd /home/amia/dev_server || exit
sudo docker-compose stop devserver2
sudo docker-compose rm -f devserver2

popd || exit