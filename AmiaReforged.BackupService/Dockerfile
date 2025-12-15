FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file
COPY ["AmiaReforged.BackupService.csproj", "./"]

# Restore dependencies
RUN dotnet restore "AmiaReforged.BackupService.csproj"

# Copy source code
COPY . .

# Build and publish
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

# Install PostgreSQL client for pg_dump
RUN apt-get update && \
    apt-get install -y --no-install-recommends postgresql-client git && \
    rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Create backup directory
RUN mkdir -p /var/backups/amia/sql

# Configure git for the container
RUN git config --global user.email "backup@amia.local" && \
    git config --global user.name "Amia Backup Service"

ENTRYPOINT ["dotnet", "AmiaReforged.BackupService.dll"]
