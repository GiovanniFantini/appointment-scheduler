#!/bin/bash
set -e

echo "Preparing deployment package..."

# Create deployment directory
rm -rf deploy
mkdir -p deploy

# Copy built files
echo "Copying dist folder..."
cp -r dist deploy/

# Copy server files
echo "Copying server.js..."
cp server.js deploy/

# Create production package.json (without devDependencies and workspace-only deps).
# Workspace packages (@scheduler/*) sono bundlati nel dist/ da Vite — a runtime
# il server Express serve solo i file statici, quindi non vanno installati su Azure.
echo "Creating production package.json..."
node -e "
const pkg = require('./package.json');
const deps = Object.fromEntries(
  Object.entries(pkg.dependencies || {}).filter(([name, version]) => {
    if (name.startsWith('@scheduler/')) return false;
    if (typeof version === 'string' && (version === '*' || version.startsWith('workspace:'))) return false;
    return true;
  })
);
const prodPkg = {
  name: pkg.name,
  version: pkg.version,
  private: pkg.private,
  type: pkg.type,
  scripts: {
    start: pkg.scripts.start
  },
  dependencies: deps
};
require('fs').writeFileSync('./deploy/package.json', JSON.stringify(prodPkg, null, 2));
"

# Copy other necessary files
echo "Copying static files..."
cp -r public deploy/ 2>/dev/null || true
cp index.html deploy/ 2>/dev/null || true

echo "Deployment package ready in ./deploy directory"
