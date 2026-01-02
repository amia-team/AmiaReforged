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

# Set UTF-8 locale for proper Unicode filename handling
ENV LANG=C.UTF-8
ENV LC_ALL=C.UTF-8

# Install PostgreSQL 18 client for pg_dump (must match server version)
RUN apt-get update && \
    apt-get install -y --no-install-recommends gnupg curl ca-certificates locales && \
    curl -fsSL https://www.postgresql.org/media/keys/ACCC4CF8.asc | gpg --dearmor -o /usr/share/keyrings/postgresql-keyring.gpg && \
    echo "deb [signed-by=/usr/share/keyrings/postgresql-keyring.gpg] http://apt.postgresql.org/pub/repos/apt bookworm-pgdg main" > /etc/apt/sources.list.d/pgdg.list && \
    apt-get update && \
    apt-get install -y --no-install-recommends postgresql-client-18 git rsync && \
    rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Create backup directory
RUN mkdir -p /var/backups/amia/sql

# Configure git for the container
RUN git config --global user.email "backup@amia.local" && \
    git config --global user.name "Amia Backup Service"

ENTRYPOINT ["dotnet", "AmiaReforged.BackupService.dll"]
