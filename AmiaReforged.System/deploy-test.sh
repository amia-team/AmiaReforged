#!/bin/bash
sudo chown -R amia.amia bin/Debug/net6.0/
# Check if the plugin directory exists, and if not, create it.
if [ ! -d "/home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.System/" ]; then
    sudo mkdir /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.System/
fi
sudo cp -r bin/Debug/net6.0/* /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.System/
sudo chown -R jenkins.jenkins bin/Debug/net7.0/
