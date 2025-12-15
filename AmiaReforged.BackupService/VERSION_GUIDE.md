# BackupService Version Update Guide

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
cd AmiaReforged.BackupService
echo "1.0.1" > version.txt
```

### 3. Update CHANGELOG.md
Add a new section at the top:
```markdown
## [1.0.1] - 2025-12-16

### Fixed
- Fixed database connection timeout issue
- Improved error handling in backup process

### Changed
- Updated backup retention policy to 30 days
```

### 4. Commit Changes
```bash
git add version.txt CHANGELOG.md
git commit -m "Bump BackupService version to 1.0.1"
git push
```

### 5. Jenkins Auto-Build
Jenkins will automatically:
- Detect changes in `AmiaReforged.BackupService/**`
- Read version from `version.txt`
- Build Docker image with tags:
  - `amia-backup-service:1.0.1`
  - `amia-backup-service:latest`

## Verification

Check the Jenkins build logs to confirm:
```
Building BackupService version 1.0.1
Docker image built successfully: amia-backup-service:1.0.1
```

## Manual Build (if needed)
```bash
cd AmiaReforged.BackupService
docker build -t amia-backup-service:$(cat version.txt) -t amia-backup-service:latest .
```

## CHANGELOG Template

```markdown
## [X.Y.Z] - YYYY-MM-DD

### Added
- New features

### Changed
- Changes to existing functionality

### Deprecated
- Soon-to-be removed features

### Removed
- Removed features

### Fixed
- Bug fixes

### Security
- Security improvements
```

## Common Scenarios

### Bug Fix
```
1.0.0 → 1.0.1
Document in CHANGELOG under "### Fixed"
```

### New Feature
```
1.0.1 → 1.1.0
Document in CHANGELOG under "### Added"
```

### Breaking Change
```
1.1.0 → 2.0.0
Document in CHANGELOG under "### Changed" or "### Removed"
Add migration guide if needed
```

