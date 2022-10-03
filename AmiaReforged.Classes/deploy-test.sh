#!/bin/bash
sudo chown -R amia.amia bin/Debug/net6.0/
sudo cp -r bin/Debug/net6.0/* /home/amia/amia_server/test_server/anvil/Plugins/Amia.Warlock/
pushd /home/amia/amia_server
docker-compose stop test-server
docker-compose rm -f test-server
docker-compose up -d
popd
sudo chown -R jenkins.jenkins bin/Debug/net6.0/
