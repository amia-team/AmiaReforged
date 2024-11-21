#!/bin/bash

if [ -z "$1" ]
then
    echo "Please provide an output directory as an argument. Example: ./publish.sh /path/to/directory"
    exit 1
fi

dotnet publish ./AmiaReforged.Classes/AmiaReforged.Classes.csproj -o $1/AmiaReforged.Classes
dotnet publish ./AmiaReforged.Races/AmiaReforged.Races.csproj -o $1/AmiaReforged.Races
dotnet publish ./AmiaReforged.System/AmiaReforged.System.csproj -o $1/AmiaReforged.System
dotnet publish ./AmiaReforged.System/AmiaReforged.PwEngine.csproj -o $1/AmiaReforged.PwEngine