#!/bin/bash
# Test runner script for API tests
# Run this from the AmiaReforged root directory

set -e

echo "=================================="
echo "  API Testing Suite"
echo "=================================="
echo ""

echo "📦 Building projects..."
dotnet build AmiaReforged.PwEngine --no-restore --verbosity quiet
dotnet build WorldSimulator.Tests --no-restore --verbosity quiet
echo "✅ Build complete"
echo ""

echo "🧪 Running PwEngine API Tests..."
dotnet test AmiaReforged.PwEngine \
  --filter "FullyQualifiedName~API.Tests" \
  --no-build \
  --verbosity normal \
  --logger "console;verbosity=normal"
echo ""

echo "🧪 Running WorldSimulator Client Tests..."
dotnet test WorldSimulator.Tests \
  --filter "FullyQualifiedName~PwEngineClient" \
  --no-build \
  --verbosity normal \
  --logger "console;verbosity=normal"
echo ""

echo "=================================="
echo "✅ All API tests complete!"
echo "=================================="

