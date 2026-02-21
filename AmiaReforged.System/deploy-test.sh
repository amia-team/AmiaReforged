#!/bin/bash
chown -R amia.amia bin/Debug/net7.0/
# Check if the plugin directory exists, and if not, create it.
if [ ! -d "/home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.System/" ]; then
    mkdir /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.System/
fi

dotnet publish AmiaReforged.System --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.System/

chown -R jenkins.jenkins bin/Debug/net7.0/
