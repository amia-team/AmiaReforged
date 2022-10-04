#!/bin/bash
sudo chown -R amia.amia bin/Debug/net6.0/
# Check if the plugin directory exists, and if not, create it.
if [ ! -d "/home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Classes/" ]; then
    sudo mkdir /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Classes/
fi
sudo cp -r bin/Debug/net6.0/* /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Classes/
pushd /home/amia/amia_server || exit
docker-compose stop test-server
docker-compose rm -f test-server
docker-compose up -d
popd || exit
sudo chown -R jenkins.jenkins bin/Debug/net6.0/ 