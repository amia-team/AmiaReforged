# Amia Admin Panel

A Blazor Server admin panel for monitoring and managing Docker containers with real-time log streaming, crash detection, and automatic restart capabilities.

## Features

- **Real-time Log Streaming**: View live container logs with syntax highlighting for errors, warnings, and crash patterns
- **Selective Container Monitoring**: Choose which containers to monitor from the UI
- **Crash Detection**: Configurable regex patterns to detect segfaults, SIGSEGV, and other crash indicators
- **Auto-Restart**: Automatically restart containers when crash patterns are detected
- **Secure Authentication**: ASP.NET Identity with single admin account
- **Let's Encrypt TLS**: Automatic HTTPS via Caddy reverse proxy

## Quick Start

### Prerequisites

- Docker and Docker Compose
- A domain pointing to your server (for Let's Encrypt)

### Deployment

1. Copy `.env.example` to `.env` and configure:

```bash
cp .env.example .env
nano .env
```

2. Set your configuration:

```env
ADMIN_DOMAIN=admin.yourdomain.com
ADMIN_EMAIL=admin@yourdomain.com
ADMIN_PASSWORD=YourSecurePassword123!
DB_PASSWORD=your_secure_db_password
```

3. Deploy with Docker Compose:

```bash
cd AmiaReforged.AdminPanel
docker-compose up -d
```

4. Access the panel at `https://admin.yourdomain.com`

Default credentials:
- Username: `admin`
- Password: (from `ADMIN_PASSWORD` env variable)

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ADMIN_DOMAIN` | Domain for TLS certificate | `admin.example.com` |
| `ADMIN_EMAIL` | Email for Let's Encrypt | `admin@example.com` |
| `ADMIN_PASSWORD` | Admin login password | `ChangeMe123!` |
| `DB_PASSWORD` | PostgreSQL password | `amia` |
| `DISCORD_WEBHOOK_URL` | Discord notifications (optional) | - |

### Watch Patterns

Default crash detection patterns (configurable per container):
- `segfault` - Segmentation faults
- `SIGSEGV` - Signal segmentation violation
- `core dumped` - Core dump messages

Custom patterns can be added via the Configure page for each monitored container.

## Architecture

```
┌─────────────────────┐     ┌─────────────────────┐
│    Caddy (TLS)      │────▶│  Admin Panel (8080) │
│    :443             │     │  Blazor Server      │
└─────────────────────┘     └─────────────────────┘
                                      │
                    ┌─────────────────┼─────────────────┐
                    ▼                 ▼                 ▼
            ┌─────────────┐   ┌─────────────┐   ┌─────────────┐
            │ PostgreSQL  │   │ Docker API  │   │  SignalR    │
            │ (Identity)  │   │ (socket)    │   │  (logs)     │
            └─────────────┘   └─────────────┘   └─────────────┘
```

## Development

### Local Development (Without Docker)

```bash
# Restore and build
dotnet restore
dotnet build

# Run with hot reload
dotnet watch run
```

### Docker Dev Mode (No TLS/Let's Encrypt)

For local testing with Docker but without Caddy/Let's Encrypt:

1. Copy the dev environment file:

```bash
cp .env.dev .env
```

2. Start with the dev override:

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

3. Access the panel at `http://localhost:8080`

This configuration:
- Exposes the admin panel directly on port 8080 (no Caddy proxy)
- Disables HTTPS/TLS entirely
- Uses development-friendly default credentials
- Sets `DOTNET_ENVIRONMENT=Development` for detailed error pages

### Database Migrations

```bash
# Add migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update
```

## Security Notes

- The Docker socket is mounted read-only for container inspection
- Restart/stop/start operations require the socket to be mounted with write access
- All routes require authentication
- Passwords are hashed with ASP.NET Identity's PBKDF2

## License

See [LICENSE](../LICENSE) in the repository root.
