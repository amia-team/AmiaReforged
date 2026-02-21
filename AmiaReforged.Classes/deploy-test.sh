#!/bin/bash
chown -R amia.amia bin/Debug/net7.0/
# Check if the plugin directory exists, and if not, create it.
if [ ! -d "/home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Classes/" ]; then
    mkdir /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Classes/
fi

dotnet publish AmiaReforged.Classes --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Classes/

chown -R jenkins.jenkins bin/Debug/net7.0/
