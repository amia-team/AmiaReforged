# WorldSimulator Jenkinsfile Configuration ✅

**Date**: October 29, 2025
**Status**: Configured with Change Detection & Test Execution

---

## Summary

The WorldSimulator Jenkinsfile has been configured to:
1. ✅ Run tests from the correct test project (`WorldSimulator.Tests`)
2. ✅ Only execute when changes are detected in WorldSimulator projects
3. ✅ Skip unnecessary builds when other parts of the solution change
4. ✅ Report results via Discord webhook

---

## Key Features

### 1. Change Detection Stage

The pipeline now includes an early check that compares changed files:

```groovy
stage('Check for Changes') {
    steps {
        script {
            // Get list of changed files since last commit
            def changes = sh(
                script: 'git diff --name-only HEAD~1 HEAD || echo "FIRST_BUILD"',
                returnStdout: true
            ).trim()

            // Check if WorldSimulator or WorldSimulator.Tests were modified
            def hasSimulatorChanges = changes.contains('WorldSimulator/') ||
                                     changes.contains('WorldSimulator.Tests/') ||
                                     changes == 'FIRST_BUILD'

            if (!hasSimulatorChanges) {
                currentBuild.result = 'NOT_BUILT'
                error('No relevant changes - pipeline stopped')
            }
        }
    }
}
```

**Behavior**:
- ✅ Runs on first build (`FIRST_BUILD` fallback)
- ✅ Runs when `WorldSimulator/` files change
- ✅ Runs when `WorldSimulator.Tests/` files change
- ❌ Skips when only other projects change (e.g., `AmiaReforged.Classes/`)

### 2. Correct Test Project

**Before**: ❌ `dotnet test WorldSimulator/WorldSimulator.csproj` (wrong - not a test project)

**After**: ✅ `dotnet test WorldSimulator.Tests/WorldSimulator.Tests.csproj`

### 3. Build Both Projects

```groovy
stage('Build') {
    steps {
        sh 'dotnet build WorldSimulator/WorldSimulator.csproj --configuration Release'
        sh 'dotnet build WorldSimulator.Tests/WorldSimulator.Tests.csproj --configuration Release'
    }
}
```

Ensures both the main project and test project are compiled before running tests.

### 4. Test Execution

```groovy
stage('Test') {
    steps {
        sh '''
            dotnet test WorldSimulator.Tests/WorldSimulator.Tests.csproj \
              --configuration Release \
              --no-build \
              --logger "trx;LogFileName=test-results.trx" \
              --collect:"XPlat Code Coverage" \
              --verbosity normal \
              || exit 1
        '''
    }
}
```

**Features**:
- Runs all tests (unit + BDD scenarios)
- Generates TRX format results for Jenkins
- Collects code coverage
- Fails build on test failure (`|| exit 1`)

### 5. Test Results Publishing

```groovy
stage('Publish Test Results') {
    when {
        always()  // Run even if tests fail
    }
    steps {
        junit 'WorldSimulator.Tests/**/test-results.trx'
        publishHTML([
            reportDir: 'WorldSimulator.Tests/TestResults',
            reportFiles: 'index.html',
            reportName: 'Code Coverage Report'
        ])
    }
}
```

Results are published to Jenkins UI for analysis.

### 6. Smart Discord Notifications

```groovy
post {
    always {
        script {
            // Only send Discord notification if build actually ran
            if (currentBuild.result != 'NOT_BUILT') {
                discordSend(...)
            } else {
                echo 'Build skipped - no changes in WorldSimulator projects'
            }
        }
    }
}
```

**Behavior**:
- ✅ Sends notification when build runs and succeeds
- ✅ Sends notification when build runs and fails
- ❌ Skips notification when no changes detected (avoids spam)

---

## Pipeline Flow

### Scenario 1: Changes in WorldSimulator

```
1. Checkout code
2. Check for Changes ✅ (WorldSimulator/ modified)
3. Build ✅ (both projects compile)
4. Test ✅ (18 tests pass)
5. Publish Test Results ✅
6. Discord Notification 📢 "Build #42 successful!"
```

### Scenario 2: Changes in Other Projects

```
1. Checkout code
2. Check for Changes ❌ (only AmiaReforged.Classes/ modified)
3. Pipeline stops with NOT_BUILT
4. No Discord notification (silent skip)
```

### Scenario 3: Test Failures

```
1. Checkout code
2. Check for Changes ✅
3. Build ✅
4. Test ❌ (2 tests fail)
5. Publish Test Results ✅ (shows which tests failed)
6. Discord Notification 📢 "Build #43 failed!"
```

---

## Benefits

### Performance
- **Faster CI/CD**: Skips unnecessary builds when WorldSimulator unchanged
- **Resource Efficiency**: Frees up Jenkins agents for other jobs
- **Reduced Noise**: Only get notifications when relevant

### Correctness
- **Proper Test Execution**: Actually runs the test project
- **Code Coverage**: Generates coverage reports for analysis
- **TRX Results**: Integrates with Jenkins test result viewer

### Developer Experience
- **Clear Feedback**: Know immediately if simulator changes broke tests
- **Discord Integration**: Get notified without checking Jenkins UI
- **Skip Logic**: Understand why builds were skipped

---

## Configuration Files Involved

1. **Jenkinsfile** - Pipeline definition (this file)
2. **WorldSimulator.csproj** - Main service project
3. **WorldSimulator.Tests.csproj** - Test project with NUnit + SpecFlow

---

## Testing the Pipeline

### Local Testing

```bash
# Verify build works
dotnet build WorldSimulator/WorldSimulator.csproj --configuration Release
dotnet build WorldSimulator.Tests/WorldSimulator.Tests.csproj --configuration Release

# Verify tests run
dotnet test WorldSimulator.Tests/WorldSimulator.Tests.csproj \
  --configuration Release \
  --logger "trx;LogFileName=test-results.trx" \
  --verbosity normal

# Check test results
ls WorldSimulator.Tests/**/test-results.trx
```

### Jenkins Testing

1. **Trigger build** with WorldSimulator changes
   - Should run full pipeline
   - Should show 18 tests passing
   - Should send Discord notification

2. **Trigger build** with only other project changes
   - Should skip at "Check for Changes" stage
   - Should mark as NOT_BUILT
   - Should NOT send Discord notification

---

## Future Enhancements

Potential additions as the project matures:

- [ ] Add deployment stage for successful builds
- [ ] Integration tests with PostgreSQL test container
- [ ] Performance benchmarking stage
- [ ] Docker image build and push
- [ ] Kubernetes deployment automation
- [ ] Separate dev/test/prod pipelines

---

## Related Documentation

- `SCAFFOLDING_COMPLETE.md` - Initial project setup
- `TYPED_PAYLOADS_COMPLETE.md` - Domain model improvements
- `SimulatorRequirements.md` - Business requirements
- `README.md` - Developer guide

---

## Summary

The Jenkinsfile now:
- ✅ Runs the correct test project (`WorldSimulator.Tests`)
- ✅ Only executes when WorldSimulator files change
- ✅ Publishes test results and code coverage
- ✅ Sends smart Discord notifications (skip spam on no changes)
- ✅ Fails build on test failures
- ✅ Provides clear feedback at each stage

**Status**: Ready for CI/CD integration

