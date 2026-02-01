# AdminPanel Version Update Guide

## Quick Steps to Release a New Version

### 1. Determine Version Bump
- **PATCH** (X.Y.Z+1): Bug fixes, minor changes
  - Example: 1.0.0 → 1.0.1
- **MINOR** (X.Y+1.0): New features, backward compatible
  - Example: 1.0.1 → 1.1.0
- **MAJOR** (X+1.0.0): Breaking changes
  - Example: 1.1.0 → 2.0.0

### 2. Update version.txt
```bash
cd AmiaReforged.AdminPanel
echo "1.0.1" > version.txt
```

### 3. Update CHANGELOG.md (if exists)
Add a new section at the top:
```markdown
## [1.0.1] - 2026-02-01

### Fixed
- Fixed container status refresh issue
- Improved error handling in Docker API calls

### Changed
- Updated UI for better container management
```

### 4. Commit Changes
```bash
git add version.txt CHANGELOG.md
git commit -m "Bump AdminPanel version to 1.0.1"
git push
```

### 5. Jenkins Auto-Build
Jenkins will automatically:
- Detect changes in `AmiaReforged.AdminPanel/**`
- Read version from `version.txt`
- Build Docker image with tags:
  - `amia-admin-panel:1.0.1`
  - `amia-admin-panel:latest`

## Verification

Check the Jenkins build logs to confirm:
```
Building AdminPanel version 1.0.1
Docker image built successfully: amia-admin-panel:1.0.1
```

## Important Notes

- The Dockerfile is built from the repository root (not the AdminPanel directory) because it needs access to shared dependencies
- Always test locally before pushing version changes
- Version format: Use semantic versioning (MAJOR.MINOR.PATCH)
