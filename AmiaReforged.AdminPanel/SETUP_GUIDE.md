# Admin Panel Setup Guide

This guide walks you through setting up the Amia Admin Panel for Docker container monitoring and management.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Quick Start (Docker Compose)](#quick-start-docker-compose)
3. [Manual Docker Build](#manual-docker-build)
4. [Local Development Setup](#local-development-setup)
5. [Configuration Reference](#configuration-reference)
6. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### For Docker Deployment
- Docker Engine 20.10+
- Docker Compose v2.0+
- A domain pointing to your server (for HTTPS via Let's Encrypt)
- Ports 80 and 443 available

### For Local Development
- .NET 8.0 SDK
- Docker socket access (for container management features)

---

## Quick Start (Docker Compose)

### Step 1: Configure Environment

Navigate to the AdminPanel directory and create your environment file:

```bash
cd AmiaReforged.AdminPanel
cp .env.example .env
```

Edit `.env` with your settings:

```bash
nano .env
```

Required settings:
```env
# Domain Configuration (for Let's Encrypt TLS)
ADMIN_DOMAIN=admin.yourdomain.com
ADMIN_EMAIL=admin@yourdomain.com

# Admin Credentials
ADMIN_USERNAME=admin
ADMIN_PASSWORD=YourSecurePassword123!

# Optional: Discord Notifications
DISCORD_WEBHOOK_URL=https://discord.com/api/webhooks/...
```

### Step 2: Deploy

```bash
docker-compose up -d
```

This starts:
- **admin-panel**: The Blazor Server application on port 8080
- **caddy**: Reverse proxy with automatic HTTPS on ports 80/443

### Step 3: Access the Panel

Open `https://admin.yourdomain.com` in your browser.

Login with:
- **Username**: (the `ADMIN_USERNAME` you set in `.env`, default: `admin`)
- **Password**: (the `ADMIN_PASSWORD` you set in `.env`)

---

## Manual Docker Build

If you prefer to build the image manually (or for CI/CD):

### From Repository Root

```bash
# Read version from version.txt
VERSION=$(cat AmiaReforged.AdminPanel/version.txt | tr -d '\n')

# Build with version tags
docker build \
  -f AmiaReforged.AdminPanel/Dockerfile \
  -t amia-admin-panel:${VERSION} \
  -t amia-admin-panel:latest \
  --build-arg BUILD_VERSION=${VERSION} \
  --build-arg GIT_SHA=$(git rev-parse HEAD) \
  --build-arg BUILD_TIME=$(date -u +"%Y-%m-%dT%H:%M:%SZ") \
  .
```

### Run Standalone (without Caddy)

```bash
docker run -d \
  --name admin-panel \
  -p 8080:8080 \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -v admin_data:/data \
  -e ADMIN_USERNAME=admin \
  -e ADMIN_PASSWORD=YourSecurePassword123! \
  amia-admin-panel:latest
```

---

## Local Development Setup

### Step 1: Clone and Build

```bash
cd AmiaReforged.AdminPanel
dotnet restore
dotnet build
```

### Step 2: Run the Application

```bash
dotnet run
```

Or with hot reload:
```bash
dotnet watch run
```

The application will be available at `http://localhost:5000` (or the port specified in `Properties/launchSettings.json`).

### Step 3: Access Docker Socket

For container management features, ensure your user has Docker socket access:

```bash
# Add your user to the docker group
sudo usermod -aG docker $USER

# Log out and back in, or run:
newgrp docker
```

---

## Configuration Reference

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ADMIN_DOMAIN` | Domain for Caddy TLS certificate | `admin.example.com` |
| `ADMIN_EMAIL` | Email for Let's Encrypt notifications | `admin@example.com` |
| `ADMIN_USERNAME` | Admin login username | `admin` |
| `ADMIN_PASSWORD` | Admin login password | `ChangeMe123!` |
| `DISCORD_WEBHOOK_URL` | Discord webhook for notifications | _(none)_ |

### Application Settings (appsettings.json)

| Setting | Description | Default |
|---------|-------------|---------|
| `AdminPanel:DefaultAdminUsername` | Admin username | `admin` |
| `AdminPanel:DefaultAdminPassword` | Admin password | `ChangeMe123!` |
| `AdminPanel:DockerSocketPath` | Docker socket path | `unix:///var/run/docker.sock` |
| `AdminPanel:ConfigPath` | Path to monitoring config JSON | `/data/monitoring-config.json` |
| `AdminPanel:ContainerPollIntervalSeconds` | Status refresh interval | `5` |
| `AdminPanel:MaxLogLinesPerContainer` | Log buffer size | `1000` |
| `AdminPanel:AutoRestartCooldownSeconds` | Cooldown between auto-restarts | `60` |

---

## Troubleshooting

### Cannot connect to Docker

**Symptom**: Error about Docker socket connection

**Solution**: Ensure the Docker socket is mounted correctly:
```yaml
volumes:
  - /var/run/docker.sock:/var/run/docker.sock
```

### Login not working

**Symptom**: Invalid credentials error

**Solution**: Check that `ADMIN_USERNAME` and `ADMIN_PASSWORD` environment variables are set correctly. Default username is `admin`.

### Configuration not persisting

**Symptom**: Monitored containers reset after restart

**Solution**: Ensure the `/data` volume is mounted:
```yaml
volumes:
  - admin_data:/data
```

### Caddy not getting certificates

**Symptom**: HTTPS not working, certificate errors

**Solution**:
1. Ensure `ADMIN_DOMAIN` points to your server's IP
2. Ensure ports 80 and 443 are open
3. Check Caddy logs: `docker-compose logs caddy`

---

## Data Persistence

The Admin Panel stores monitoring configuration in `/data/monitoring-config.json`. This is mounted as a Docker volume (`admin_data`) to persist across container restarts.

To backup:
```bash
docker cp admin-panel:/data/monitoring-config.json ./backup-config.json
```

To restore:
```bash
docker cp ./backup-config.json admin-panel:/data/monitoring-config.json
```
