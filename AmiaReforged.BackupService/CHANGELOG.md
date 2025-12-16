# AmiaReforged.BackupService Changelog

## [1.0.2] - 2025-12-15

### Added
- Docker Compose configuration file (`docker-compose.yml`)
- Environment variable template file (`.env.example`)
- Comprehensive documentation for Docker deployment

### Documentation
- Updated README with correct environment variable names (`GIT_USER` and `GIT_TOKEN`)
- Added documentation for environment variable overrides (e.g., `Backup__IntervalMinutes=30`)
- Added instructions for initializing the backup git repository

### Notes
- LibGit2Sharp requires explicit git credentials - it does NOT use system git credential helpers
- The backup volume must contain an initialized git repository with a configured remote

## [1.0.0] - 2025-12-15

### Added
- Initial release of the Backup Service
- Automated PostgreSQL database backups using pg_dump
- Git-based version control for backups
- Configurable backup schedules via configuration
- Support for multiple database backups
- Serilog-based structured logging
- Docker containerization support
- LightInject dependency injection
- Environment variable configuration support

### Features
- **Database Backup**: Automated SQL dump creation for PostgreSQL databases
- **Version Control**: Automatic Git commits for backup versioning
- **Scheduling**: Configurable backup intervals
- **Logging**: Comprehensive logging with Serilog
- **Configuration**: Flexible configuration via appsettings.json and environment variables

### Technical Details
- Built on .NET 8.0
- Runs as a background worker service
- Includes PostgreSQL client and Git in runtime image
- Uses LibGit2Sharp for Git operations

