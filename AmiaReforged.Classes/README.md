# Warlock Plugin

This is a plugin for [NWN.Anvil](https://github.com/nwn-dotnet/Anvil). This repository houses all of the code necessary
for Warlock to function, assuming Amia's haks are installed and used correctly.

## Installation

To install this plugin, you must build it using `dotnet build`. Once the build is complete, you must copy the contents
of `bin/Debug/net6.0/bin/` to the `anvil/Plugins/Amia.Warlock/` directory of the server in question. The directory must
be named the same as the project's compiled DLL or the plugin will not function.