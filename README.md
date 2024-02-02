# AmiaReforged

This repository for all of Amia's NWN.Anvil plugins. Ultimately, all projects will have a common reference in
AmiaReforged.Core as certain concepts and ideas end up being abstracted away from NWN to allow for more testability.

## Running Migrations with Entity Framework

### Prerequisites

- Docker environment is running
- .Net 8 SDK
- .Net 8 Runtime
- .Net 8 EF Tools

### How to check if you have the .Net 8 SDK installed

Run the following command in your terminal:

```bash
dotnet --list-sdks
```

### Installing EF Tools

If you don't have the EF tools installed, you can install them with the following command:

```bash
dotnet tool install --global dotnet-ef
```

### Running Migrations

Migrations must be run from AmiaReforged.Core, as the migrations belong to that project at the moment.

Run the following command to update the database:

```bash
cd AmiaReforged.Core
dotnet ef database update --connection "Server=localhost:port;Database=amia;Username=amia;Password=mypassword;"
```

The port is the port of the database server, which is 5432 by default for PostgreSQL. The database name, username, and
password should be set to the appropriate values for your environment (which should be configured
in `amia_server_environment/config/`).

#### Generating Migrations

```bash
cd AmiaReforged.Core
dotnet ef migrations add <migration_name> --context AmiaDbContext
```

#### Disclaimer on Migrations

You cannot run the migrations for the production database from your local environment. The production database is
managed
by the host machine. You can only run migrations for the development database from your local environment.

## Installing the plugins

Amia uses NWN.Anvil to manage its plugins. To install the plugins, you need to copy the compiled plugins to the
`anvil/plugins` directory of your server environment. The plugins are compiled to the `bin/Debug/net7.0` directory of
each project. You can build and deploy the plugins to the server environment with the `dotnet publish` command.

```bash
dotnet publish AmiaReforged.PluginName --output "C:\absolute\path\to\anvil\plugins\AmiaReforged.PluginName"
```

> Hint: "AmiaReforged.PluginName" is the name of the plugin you want to deploy and must match the name of the target
> directory. If the folder doesn't exist, it will make the folder. You do not need to make the folder yourself.



It is imperative that the project name and output folder name are the exact same, otherwise Anvil will not know what to
do with the contents of the folder.