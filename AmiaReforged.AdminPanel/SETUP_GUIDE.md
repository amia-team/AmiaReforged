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
- PostgreSQL 14+ (or Docker for containerized DB)
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
ADMIN_PASSWORD=YourSecurePassword123!

# Database
DB_PASSWORD=your_secure_db_password

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
- **postgres**: PostgreSQL database for identity/settings

### Step 3: Access the Panel

Open `https://admin.yourdomain.com` in your browser.

Login with:
- **Username**: `admin`
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
  --build-arg GIT_SHA=$(git rev-parse --short HEAD) \
  --build-arg BUILD_TIME=$(date -u +%Y-%m-%dT%H:%M:%SZ) \
  .

echo "Built amia-admin-panel:${VERSION}"
```

### Run the Container

```bash
docker run -d \
  --name admin-panel \
  -p 8080:8080 \
  -v /var/run/docker.sock:/var/run/docker.sock:ro \
  -e ConnectionStrings__DefaultConnection="Host=your-db-host;Database=admin_panel;Username=amia;Password=your_password" \
  -e AdminPanel__DefaultAdminPassword="YourSecurePassword123!" \
  amia-admin-panel:latest
```

---

## Local Development Setup

### Step 1: Install Dependencies

```bash
cd AmiaReforged.AdminPanel
dotnet restore
```

### Step 2: Start PostgreSQL

Using Docker:
```bash
docker run -d \
  --name admin-panel-db \
  -p 5432:5432 \
  -e POSTGRES_USER=amia \
  -e POSTGRES_PASSWORD=amia \
  -e POSTGRES_DB=admin_panel \
  postgres:16
```

### Step 3: Apply Database Migrations

```bash
dotnet ef database update
```

### Step 4: Run the Application

```bash
dotnet run
```

Or with hot reload:
```bash
dotnet watch run
```

The application will be available at `http://localhost:5000` (or the port specified in `Properties/launchSettings.json`).

### Step 5: Access Docker Socket

For container management features, ensure your user has Docker socket access:

```bash
# Add your user to the docker group
usermod -aG docker $USER

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
| `ADMIN_PASSWORD` | Initial admin account password | `ChangeMe123!` |
| `DB_PASSWORD` | PostgreSQL password | `amia` |
| `DISCORD_WEBHOOK_URL` | Discord webhook for notifications | _(none)_ |

### Application Settings (appsettings.json)

| Setting | Description | Default |
|---------|-------------|---------|
| `AdminPanel:DefaultAdminUsername` | Admin username | `admin` |
| `AdminPanel:DefaultAdminEmail` | Admin email | `admin@amia.local` |
| `AdminPanel:DefaultAdminPassword` | Admin password | `ChangeMe123!` |
| `AdminPanel:DockerSocketPath` | Docker socket path | `unix:///var/run/docker.sock` |
| `AdminPanel:ContainerPollIntervalSeconds` | Status refresh interval | `5` |
| `AdminPanel:MaxLogLinesPerContainer` | Log buffer size | `1000` |
| `AdminPanel:AutoRestartCooldownSeconds` | Cooldown between auto-restarts | `60` |

### Caddy Configuration

The `Caddyfile` configures the reverse proxy:

```caddyfile
{$ADMIN_DOMAIN} {
    reverse_proxy admin-panel:8080

    # Security headers included:
    # - X-Content-Type-Options: nosniff
    # - X-Frame-Options: DENY
    # - X-XSS-Protection: 1; mode=block
    # - Referrer-Policy: strict-origin-when-cross-origin
}
```

---

## Troubleshooting

### Cannot Connect to Docker Socket

**Symptom**: "Cannot connect to Docker daemon" errors

**Solution**:
```bash
# Check socket permissions
ls -la /var/run/docker.sock

# Ensure container has socket mounted
docker inspect admin-panel | grep -A5 "Mounts"
```

For write access (restart/stop/start), mount without `:ro`:
```yaml
volumes:
  - /var/run/docker.sock:/var/run/docker.sock
```

### Database Connection Failed

**Symptom**: "Connection refused" or timeout errors

**Solution**:
1. Verify PostgreSQL is running:
   ```bash
   docker ps | grep postgres
   ```

2. Check connection string in environment:
   ```bash
   docker exec admin-panel printenv | grep Connection
   ```

3. Ensure database exists:
   ```bash
   docker exec -it admin-panel-db psql -U amia -c "\l"
   ```

### Let's Encrypt Certificate Issues

**Symptom**: HTTPS not working or certificate errors

**Solution**:
1. Ensure ports 80 and 443 are open on your firewall
2. Verify DNS is pointing to your server:
   ```bash
   dig +short admin.yourdomain.com
   ```
3. Check Caddy logs:
   ```bash
   docker logs caddy
   ```

### Admin Login Not Working

**Symptom**: Cannot login with default credentials

**Solution**:
1. Verify the password was set correctly:
   ```bash
   docker exec admin-panel printenv | grep ADMIN_PASSWORD
   ```

2. The admin account is created on first startup. If you need to reset:
   ```bash
   # Stop the container
   docker-compose down

   # Remove the database volume
   docker volume rm adminpanel_postgres_data

   # Restart with correct password
   docker-compose up -d
   ```

---

## Next Steps

- Review the [VERSION_GUIDE.md](VERSION_GUIDE.md) for release procedures
- Configure crash detection patterns for your containers
- Set up Discord webhook for notifications
- Add additional containers to monitor via the UI

## Support

For issues, check the repository's issue tracker or review logs:

```bash
# Admin Panel logs
docker logs -f admin-panel

# All services
docker-compose logs -f
```
