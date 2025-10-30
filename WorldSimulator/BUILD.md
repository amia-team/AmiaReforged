# WorldSimulator Docker Build Guide

## Overview

The `build.sh` script provides a semi-agnostic, production-ready way to build versioned Docker images for WorldSimulator. It handles:

- **Automatic version detection** from git branch/SHA
- **Semantic versioning** support for releases
- **Multiple image tagging** for flexible deployment strategies
- **Build metadata** (Git SHA, timestamps, version) embedded in images
- **OCI image labels** for traceability and compliance
- **Color-coded output** for easy readability

## Quick Start

### Basic Build (Auto-detect version)

```bash
cd WorldSimulator
./build.sh
```

This will:
- Detect version from current git branch (main → stable, develop → dev timestamp)
- Build image as `worldsim:<version>`
- Embed Git SHA and build timestamp in metadata

### Build with Specific Version

```bash
./build.sh -v 1.0.0
```

Build with version `1.0.0`:
- Creates image: `worldsim:1.0.0`
- Embeds metadata with version, Git SHA, and build time

### Build with Multiple Tags

```bash
./build.sh -v 1.0.0 -t latest -t stable
```

Creates:
- `worldsim:1.0.0` (primary tag)
- `worldsim:latest` (secondary tag)
- `worldsim:stable` (tertiary tag)

All tags point to the same image with identical metadata.

### Build Without Cache

```bash
./build.sh --no-cache -v rebuild
```

Forces Docker to rebuild all layers without using cache.

## Version Auto-Detection

When no `-v` flag is provided, the script detects the version based on current git branch:

| Branch          | Example Version      | Use Case              |
|-----------------|----------------------|-----------------------|
| `main`/`master` | `stable-143bea34`    | Production releases   |
| `develop`/`dev` | `dev-20251030144500` | Development builds    |
| `feature/*`     | `feature-featurename-timestamp` | Feature branches |
| Other branches  | `feature-branch-timestamp` | Custom branches |

## Semantic Versioning

For releases, use semantic versioning:

```bash
# Alpha release
./build.sh -v 1.0.0-alpha

# Beta release
./build.sh -v 1.0.0-beta -t beta

# Release candidate
./build.sh -v 1.0.0-rc.1 -t rc

# Production release
./build.sh -v 1.0.0 -t latest -t stable
```

## Usage Examples

### Development Workflow

```bash
# Build development version
./build.sh

# Build with explicit dev tag
./build.sh -v dev-$(date +%s) -t dev
```

### CI/CD Pipeline

```bash
#!/bin/bash
# For pull requests
if [ "$GITHUB_REF" = "refs/heads/main" ]; then
    WorldSimulator/build.sh -v ${{ github.sha }} -t latest
elif [ "$GITHUB_REF" = "refs/heads/develop" ]; then
    WorldSimulator/build.sh -v dev-${{ github.run_id }}
fi
```

### Release Process

```bash
# Tag the release
git tag v1.2.3

# Build release image
WorldSimulator/build.sh -v 1.2.3 -t latest -t stable

# Push to registry (optional)
docker tag worldsim:1.2.3 myregistry.azurecr.io/worldsim:1.2.3
docker push myregistry.azurecr.io/worldsim:1.2.3
```

### Local Development

```bash
# Quick rebuild with current version
./build.sh

# Rebuild without cache after dependency changes
./build.sh --no-cache

# Named builds for different branches
git checkout feature/my-feature
./build.sh -v feature-my-feature-local
```

## Image Metadata

Each built image contains OCI-compliant labels:

```bash
docker inspect worldsim:1.0.0 --format='
Version:   {{index .Config.Labels "org.opencontainers.image.version"}}
Revision:  {{index .Config.Labels "org.opencontainers.image.revision"}}
Created:   {{index .Config.Labels "org.opencontainers.image.created"}}
Title:     {{index .Config.Labels "org.opencontainers.image.title"}}
'
```

## Build Arguments Injected

The script automatically injects:

| Argument | Source | Example |
|----------|--------|---------|
| `GIT_SHA` | `git rev-parse --short HEAD` | `143bea34` |
| `BUILD_TIME` | `date -u +%Y-%m-%dT%H:%M:%SZ` | `2025-10-30T17:44:55Z` |
| `BUILD_VERSION` | User-provided or auto-detected | `1.0.0-dev` |

These are embedded in the Docker image labels and can be inspected at runtime.

## Docker Compose Integration

Update your `docker-compose.yml` to rebuild:

```yaml
services:
  worldsim:
    image: worldsim:latest
    # Image will be used from local Docker daemon
    # Run ./build.sh to create/update it
```

Then use the build script to update:

```bash
./build.sh -v latest-build -t latest
docker compose up -d
```

## Common Commands

```bash
# View all worldsim images
docker images worldsim

# Get detailed metadata
docker inspect worldsim:1.0.0

# Run a version
docker run -it --rm \
  -e ConnectionStrings__DefaultConnection="..." \
  worldsim:1.0.0

# Tag an existing image
docker tag worldsim:1.0.0 worldsim:production

# Remove old versions
docker rmi worldsim:old-version
```

## Troubleshooting

### "Dockerfile not found"
```bash
# Make sure you're in the right directory
cd WorldSimulator
./build.sh
```

### "Git command not found"
```bash
# The script falls back to "unknown" if git isn't available
# This is normal in CI/CD environments without git
```

### "Docker command not found"
```bash
# Ensure Docker is installed and running
docker --version
```

### Build takes too long
```bash
# Use cache intelligently
./build.sh -v my-version  # Uses cache
./build.sh --no-cache -v my-version  # Rebuilds everything
```

## Best Practices

1. **Always use versioning** - Makes it easy to rollback
2. **Use meaningful tags** - `1.0.0`, `stable`, `latest` are clearer than `test1`, `build2`
3. **Tag releases** - Always create `latest` tag for the current production version
4. **Embed metadata** - The script does this automatically; inspect with `docker inspect`
5. **Document versions** - Keep a CHANGELOG.md tracking what changed between versions
6. **Test before tagging** - Build, test, then tag as `latest`

## Environment Variables

The script respects standard Docker environment variables:

```bash
# Use a specific Docker daemon
DOCKER_HOST=unix:///var/run/docker.sock ./build.sh

# Use custom registry (for push operations)
DOCKER_REGISTRY=myregistry.azurecr.io ./build.sh -v 1.0.0
```

## Next Steps

Once your image is built:

1. **Test locally**: `docker run -it worldsim:1.0.0`
2. **Push to registry**: `docker push myregistry/worldsim:1.0.0`
3. **Deploy**: Update docker-compose.yml or Kubernetes manifests
4. **Monitor**: Check logs with `docker logs` or your logging system

## Questions?

- Check the script's help: `./build.sh --help`
- Review script comments: `cat build.sh | grep "^#"`
- Test your version detection: `./build.sh` (without -v flag)

