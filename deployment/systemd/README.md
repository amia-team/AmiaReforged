# Amia Backup Service Deployment

This directory contains systemd unit files for the Amia Backup Service.

## Files

- `amia-backup.service` - systemd service unit for the backup service

## Installation

### 1. Build the Backup Service

```bash
cd /path/to/AmiaReforged
dotnet publish AmiaReforged.BackupService -c Release -o /opt/amia/backup-service
```

### 2. Create Environment File

Create `/opt/amia/backup-service/.env` with your database credentials:

```bash
# Database: amia
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DB=amia
POSTGRES_USER=amia
POSTGRES_PASSWORD=your_password

# Database: pw_engine
PW_HOST=localhost
PW_PORT=5432
PW_DB=pw_engine
PW_USER=amia
PW_PASSWORD=your_password

# Database: simulation (if deployed)
SIM_HOST=localhost
SIM_PORT=5432
SIM_DB=world_simulation
SIM_USER=amia
SIM_PASSWORD=your_password

# Git credentials for pushing backups
GIT_USER=your_git_username
GIT_TOKEN=your_git_personal_access_token
```

### 3. Initialize Backup Repository

```bash
# Create and initialize the backup repository
sudo mkdir -p /var/backups/amia/sql
cd /var/backups/amia
sudo git init
sudo git remote add origin https://github.com/your-org/amia-db-backups.git

# Set ownership
sudo chown -R amia:amia /var/backups/amia
```

### 4. Install the systemd Service

```bash
# Copy service file
sudo cp deployment/systemd/amia-backup.service /etc/systemd/system/

# Reload systemd
sudo systemctl daemon-reload

# Enable and start the service
sudo systemctl enable amia-backup.service
sudo systemctl start amia-backup.service

# Check status
sudo systemctl status amia-backup.service

# View logs
sudo journalctl -u amia-backup.service -f
```

## Docker Alternative

If running via Docker instead of systemd:

```bash
# Build the image
docker build -f AmiaReforged.BackupService/Dockerfile -t amia-backup-service .

# Run with environment variables and mounted backup volume
docker run -d \
  --name amia-backup \
  --env-file /opt/amia/backup-service/.env \
  -v /var/backups/amia:/var/backups/amia \
  amia-backup-service
```

## Configuration

Edit `appsettings.json` to customize:

- `Backup:IntervalMinutes` - Backup frequency (default: 60)
- `Backup:BackupDirectory` - Where SQL files are written
- `Backup:GitRepositoryPath` - Git repo root
- `Backup:GitRemote` - Remote name (default: origin)
- `Backup:GitBranch` - Branch to push to (default: main)
- `Backup:Databases` - List of databases to back up
