#!/bin/bash
sudo chown -R amia.amia bin/Debug/net7.0/
# Check if the plugin directory exists, and if not, create it.
if [ ! -d "/home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Core/" ]; then
    sudo mkdir /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Core/
fi
sudo cp -r bin/Debug/net7.0/* /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Core/
sudo chown -R jenkins.jenkins bin/Debug/net7.0/
