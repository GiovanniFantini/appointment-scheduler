#!/bin/bash
# Script to extract version information from Git
# Can be used in both local development and CI/CD

# Get git commit SHA (short)
GIT_COMMIT_SHA=${GIT_COMMIT_SHA:-$(git rev-parse --short HEAD 2>/dev/null || echo "dev")}

# Get git branch
GIT_BRANCH=${GIT_BRANCH:-$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "unknown")}

# Get latest git tag (semantic version)
GIT_TAG=${GIT_TAG:-$(git describe --tags --abbrev=0 2>/dev/null || echo "v0.0.0")}

# Get build number (from CI or count commits)
BUILD_NUMBER=${BUILD_NUMBER:-$(git rev-list --count HEAD 2>/dev/null || echo "0")}

# Get build timestamp
BUILD_TIME=${BUILD_TIME:-$(date -u +"%Y-%m-%dT%H:%M:%SZ")}

# Clean tag (remove 'v' prefix)
VERSION=${GIT_TAG#v}

# Output as JSON
cat <<EOF
{
  "version": "$VERSION",
  "commit": "$GIT_COMMIT_SHA",
  "branch": "$GIT_BRANCH",
  "buildNumber": "$BUILD_NUMBER",
  "buildTime": "$BUILD_TIME"
}
EOF
