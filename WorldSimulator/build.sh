#!/bin/bash

###############################################################################
# WorldSimulator Docker Build Script
#
# Usage: ./build.sh [OPTIONS]
#
# Options:
#   -v, --version VERSION      Set image version (default: auto-detect from git)
#   -t, --tag TAG              Additional image tag (can be used multiple times)
#   --no-cache                 Build without using Docker cache
#   -h, --help                 Show this help message
#
# Examples:
#   ./build.sh                           # Build with auto version
#   ./build.sh -v 1.0.0                  # Build with specific version
#   ./build.sh -v 1.0.0 -t latest       # Build with version and tag as latest
#   ./build.sh --no-cache -v dev-build  # Build without cache
#
###############################################################################

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"  # AmiaReforged root (parent of WorldSimulator)
DOCKERFILE="${SCRIPT_DIR}/Dockerfile"
IMAGE_NAME="worldsim"
BUILD_CACHE=""
ADDITIONAL_TAGS=()

# Functions
log_info() {
    echo -e "${BLUE}ℹ${NC} $1"
}

log_success() {
    echo -e "${GREEN}✓${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}⚠${NC} $1"
}

log_error() {
    echo -e "${RED}✗${NC} $1"
}

show_help() {
    head -n 23 "$0" | tail -n 22
}

get_git_sha() {
    cd "${PROJECT_ROOT}"
    git rev-parse --short HEAD 2>/dev/null || echo "unknown"
}

get_git_branch() {
    cd "${PROJECT_ROOT}"
    git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "unknown"
}

get_auto_version() {
    local branch=$(get_git_branch)
    local sha=$(get_git_sha)
    local timestamp=$(date +%Y%m%d%H%M%S)

    case "$branch" in
        main|master)
            echo "stable-${sha}"
            ;;
        develop|dev)
            echo "dev-${timestamp}"
            ;;
        *)
            echo "feature-${branch}-${timestamp}"
            ;;
    esac
}

validate_dockerfile() {
    if [ ! -f "$DOCKERFILE" ]; then
        log_error "Dockerfile not found at: $DOCKERFILE"
        exit 1
    fi
    log_success "Dockerfile found"
}

build_image() {
    local version=$1
    local sha=$(get_git_sha)
    local build_time=$(date -u +%Y-%m-%dT%H:%M:%SZ)

    log_info "Building Docker image..."
    log_info "  Image: ${IMAGE_NAME}:${version}"
    log_info "  Git SHA: ${sha}"
    log_info "  Build Time: ${build_time}"

    # Build arguments
    local build_args=(
        "--build-arg" "GIT_SHA=${sha}"
        "--build-arg" "BUILD_TIME=${build_time}"
        "--build-arg" "BUILD_VERSION=${version}"
    )

    # Add cache control
    if [ -n "$BUILD_CACHE" ]; then
        build_args+=("$BUILD_CACHE")
    fi

    # Build the image
    docker build \
        "${build_args[@]}" \
        -f "$DOCKERFILE" \
        -t "${IMAGE_NAME}:${version}" \
        "$PROJECT_ROOT" \
        || {
            log_error "Docker build failed"
            exit 1
        }

    log_success "Image built: ${IMAGE_NAME}:${version}"

    # Tag with additional tags
    for tag in "${ADDITIONAL_TAGS[@]}"; do
        log_info "Tagging as: ${IMAGE_NAME}:${tag}"
        docker tag "${IMAGE_NAME}:${version}" "${IMAGE_NAME}:${tag}"
        log_success "Tagged: ${IMAGE_NAME}:${tag}"
    done
}

show_image_info() {
    local version=$1
    echo ""
    log_info "Image Information:"
    echo ""
    docker inspect "${IMAGE_NAME}:${version}" --format='
  Image ID:     {{.ID}}
  Size:         {{.Size}} bytes
  Created:      {{.Created}}
  Architecture: {{.Architecture}}
  OS:           {{.Os}}

  Labels:
    Version:     {{index .Config.Labels "org.opencontainers.image.version"}}
    Revision:    {{index .Config.Labels "org.opencontainers.image.revision"}}
    Created:     {{index .Config.Labels "org.opencontainers.image.created"}}
    Title:       {{index .Config.Labels "org.opencontainers.image.title"}}
' || true
    echo ""
}

# Parse arguments
VERSION=""
NO_CACHE_FLAG=""

while [[ $# -gt 0 ]]; do
    case $1 in
        -v|--version)
            VERSION="$2"
            shift 2
            ;;
        -t|--tag)
            ADDITIONAL_TAGS+=("$2")
            shift 2
            ;;
        --no-cache)
            BUILD_CACHE="--no-cache"
            log_info "Building without cache"
            shift
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            log_error "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Auto-detect version if not provided
if [ -z "$VERSION" ]; then
    VERSION=$(get_auto_version)
    log_info "Auto-detected version: ${VERSION}"
fi

# Always append 'latest' to additional tags unless already present
if [[ " ${ADDITIONAL_TAGS[*]} " != *" latest "* ]]; then
    ADDITIONAL_TAGS+=("latest")
    log_info "Appended tag: latest"
fi

# Validate
if [ -z "$VERSION" ]; then
    log_error "Failed to determine version"
    exit 1
fi

log_info "WorldSimulator Docker Build Script"
echo ""

# Run build
validate_dockerfile
build_image "$VERSION"
show_image_info "$VERSION"

log_success "Build completed successfully!"
echo ""
log_info "To run the image:"
echo "  docker run -it --rm \\
    -e ConnectionStrings__DefaultConnection='...' \\
    -e PwEngine__BaseUrl='http://...' \\
    -e PwEngine__ApiKey='...' \\
    ${IMAGE_NAME}:${VERSION}"
echo ""

# Show all tags
echo "Available tags:"
docker images "${IMAGE_NAME}" --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}\t{{.Created}}" | grep -v "REPOSITORY" || true

