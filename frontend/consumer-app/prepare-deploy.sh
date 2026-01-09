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

# Create production package.json (without devDependencies)
echo "Creating production package.json..."
node -e "
const pkg = require('./package.json');
const prodPkg = {
  name: pkg.name,
  version: pkg.version,
  private: pkg.private,
  type: pkg.type,
  scripts: {
    start: pkg.scripts.start
  },
  dependencies: pkg.dependencies
};
require('fs').writeFileSync('./deploy/package.json', JSON.stringify(prodPkg, null, 2));
"

# Copy other necessary files
echo "Copying static files..."
cp -r public deploy/ 2>/dev/null || true
cp index.html deploy/ 2>/dev/null || true

echo "Deployment package ready in ./deploy directory"
