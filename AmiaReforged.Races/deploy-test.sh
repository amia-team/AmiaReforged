#!/bin/bash
chown -R amia.amia bin/Debug/net7.0/
# Check if the plugin directory exists, and if not, create it.
if [ ! -d "/home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Races/" ]; then
    mkdir /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Races/
fi

dotnet publish AmiaReforged.Races --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Races/

chown -R jenkins.jenkins bin/Debug/net7.0/
