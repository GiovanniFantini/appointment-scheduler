#!/bin/bash
# Azure Web App startup script for Node.js application
set -e

echo "Starting Admin App..."
echo "NODE_ENV: $NODE_ENV"
echo "PORT: $PORT"
echo "API_URL: $API_URL"

# Start the Node.js server
node server.js
