@echo off

IF "%~1"=="" (
    echo Please provide an output directory as an argument. Example: publish.bat C:\server\anvil\Plugins\
    exit /b
)

dotnet publish .\AmiaReforged.Classes\AmiaReforged.Classes.csproj -o %1\AmiaReforged.Classes
dotnet publish .\AmiaReforged.Races\AmiaReforged.Races.csproj -o %1\AmiaReforged.Races
dotnet publish .\AmiaReforged.System\AmiaReforged.System.csproj -o %1\AmiaReforged.System