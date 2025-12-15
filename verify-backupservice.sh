#!/bin/bash
# Verification script for BackupService Jenkins integration

set -e

echo "=== BackupService Jenkins Integration Verification ==="
echo

# Check if we're in the right directory
if [ ! -d "AmiaReforged.BackupService" ]; then
    echo "❌ Error: Must run from AmiaReforged root directory"
    exit 1
fi

echo "✓ Running from correct directory"

# Check version files exist
echo
echo "Checking version files..."
if [ ! -f "AmiaReforged.BackupService/version.txt" ]; then
    echo "❌ Missing: version.txt"
    exit 1
fi
echo "✓ version.txt exists"

if [ ! -f "AmiaReforged.BackupService/CHANGELOG.md" ]; then
    echo "❌ Missing: CHANGELOG.md"
    exit 1
fi
echo "✓ CHANGELOG.md exists"

# Read and display version
VERSION=$(cat AmiaReforged.BackupService/version.txt)
echo
echo "Current version: $VERSION"

# Check Dockerfile
echo
echo "Checking Dockerfile..."
if [ ! -f "AmiaReforged.BackupService/Dockerfile" ]; then
    echo "❌ Missing: Dockerfile"
    exit 1
fi
echo "✓ Dockerfile exists"

# Check .dockerignore
if [ ! -f "AmiaReforged.BackupService/.dockerignore" ]; then
    echo "❌ Missing: .dockerignore"
    exit 1
fi
echo "✓ .dockerignore exists"

# Verify version.txt is NOT in .dockerignore
if grep -q "^version.txt$" AmiaReforged.BackupService/.dockerignore 2>/dev/null; then
    echo "❌ version.txt should not be excluded by .dockerignore"
    exit 1
fi
echo "✓ version.txt will be included in Docker build"

# Check Jenkinsfile
echo
echo "Checking Jenkinsfile..."
if ! grep -q "Build BackupService Docker Image" Jenkinsfile; then
    echo "❌ BackupService build stage not found in Jenkinsfile"
    exit 1
fi
echo "✓ Docker build stage exists"

if ! grep -q "changeset \"AmiaReforged.BackupService/\*\*\"" Jenkinsfile; then
    echo "❌ Changeset condition not found"
    exit 1
fi
echo "✓ Changeset condition configured"

# Check that BackupService is NOT in deployment stages
if grep -A 50 "stage('Deploy Test')" Jenkinsfile | grep -q "AmiaReforged.BackupService"; then
    echo "❌ BackupService should not be in Test deployment"
    exit 1
fi
echo "✓ BackupService not in Test deployment"

if grep -A 50 "stage('Deploy Live')" Jenkinsfile | grep -q "AmiaReforged.BackupService"; then
    echo "❌ BackupService should not be in Live deployment"
    exit 1
fi
echo "✓ BackupService not in Live deployment"

# Test Docker build
echo
echo "Testing Docker build..."
cd AmiaReforged.BackupService

if docker build -q -t amia-backup-service:test . > /dev/null 2>&1; then
    echo "✓ Docker build successful"
    docker rmi -f amia-backup-service:test > /dev/null 2>&1
else
    echo "❌ Docker build failed"
    exit 1
fi

cd ..

echo
echo "=== All Checks Passed! ==="
echo
echo "Summary:"
echo "- Version tracking configured: $VERSION"
echo "- Docker build working"
echo "- Jenkins integration ready"
echo "- BackupService properly separated from Anvil plugins"
echo
echo "Next steps:"
echo "1. Commit changes: git add -A && git commit -m 'Configure BackupService Jenkins integration'"
echo "2. Push to trigger build: git push"
echo "3. Monitor Jenkins for automatic Docker image build"

