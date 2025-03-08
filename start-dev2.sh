#!/bin/bash
# Restarts the test server
pushd /home/amia/dev_server || exit
sudo docker-compose up -d devserver2
popd || exit