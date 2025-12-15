# AmiaReforged.BackupService

A background service for automated database backups with Git-based version control.

## Features

- Automated PostgreSQL database backups using `pg_dump`
- Git-based version control for backup files
- Configurable backup schedules
- Multiple database support
- Structured logging with Serilog
- Docker containerization

## Versioning

This service uses semantic versioning (MAJOR.MINOR.PATCH).

### How to Update Version

1. **Edit `version.txt`**: Update the version number (e.g., `1.0.0` → `1.0.1`)
2. **Update `CHANGELOG.md`**: Document your changes under a new version heading
3. **Commit changes**: Include the version.txt and CHANGELOG.md in your commit

Example CHANGELOG.md entry:
```markdown
## [1.0.1] - 2025-12-16

### Fixed
- Fixed connection timeout handling
- Improved error logging

### Changed
- Updated backup retention policy
```

### Version Number Guidelines

- **MAJOR**: Breaking changes or major feature releases
- **MINOR**: New features, backward compatible
- **PATCH**: Bug fixes, backward compatible

## Docker Build

The Jenkins pipeline automatically builds a Docker image when changes are detected in the `AmiaReforged.BackupService/` directory.

### Manual Build

```bash
cd AmiaReforged.BackupService
docker build -t amia-backup-service:$(cat version.txt) .
```

### Image Tags

The Jenkins build creates two tags:
- `amia-backup-service:X.Y.Z` (specific version from version.txt)
- `amia-backup-service:latest` (always points to the most recent build)

## Configuration

Configure the service using `appsettings.json` or environment variables:

```json
{
  "BackupConfig": {
    "BackupInterval": "0 2 * * *",
    "GitRemote": "origin",
    "GitBranch": "main",
    "Databases": [
      {
        "Name": "amia_db",
        "Host": "localhost",
        "Port": 5432,
        "Username": "backup_user"
      }
    ]
  }
}
```

## Running the Container

```bash
docker run -d \
  --name amia-backup \
  -v /path/to/backups:/var/backups/amia \
  -v /path/to/.env:/app/.env:ro \
  --restart unless-stopped \
  amia-backup-service:latest
```

## Environment Variables

Create a `.env` file with sensitive configuration:

```env
DB_PASSWORD=your_secure_password
GIT_USERNAME=backup-bot
GIT_TOKEN=your_git_token
```

## Development

### Requirements

- .NET 8.0 SDK
- PostgreSQL client tools
- Git

### Build

```bash
dotnet build
```

### Run Locally

```bash
dotnet run
```

## Logging

Logs are written to console in structured JSON format. Configure log levels in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

## Dependencies

- **Microsoft.Extensions.Hosting**: Background service infrastructure
- **LibGit2Sharp**: Git operations
- **Serilog**: Structured logging
- **LightInject**: Dependency injection
- **DotNetEnv**: Environment variable loading

## License

See LICENSE file in the root of the repository.

